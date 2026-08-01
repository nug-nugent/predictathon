using FluentResults;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Common;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Services;

[ScopedService]
public class FixtureImportService : IFixtureImportService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IExternalMatchDataService _externalMatchDataService;

    public FixtureImportService(IApplicationDbContext dbContext, IExternalMatchDataService externalMatchDataService)
    {
        _dbContext = dbContext;
        _externalMatchDataService = externalMatchDataService;
    }

    /// <inheritdoc />
    public async Task<Result<FixtureImportSummary>> ImportSeasonAsync(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var competition = await _dbContext.Competition.FirstOrDefaultAsync(c => c.CompetitionID == competitionId, cancellationToken);
        if (competition is null)
        {
            return Result.Fail<FixtureImportSummary>(new NotFoundError("The competition could not be found."));
        }

        if (string.IsNullOrEmpty(competition.ExternalApiCompetitionCode))
        {
            return Result.Fail<FixtureImportSummary>(new ConflictError(
                "Set an external competition code (e.g. \"PL\") on this competition before importing fixtures."));
        }

        // The season query parameter selects a single season instance (e.g. "PL 2026/27" rather than
        // the provider's whole history) - the admin's approximate StartDate is the natural source for
        // it, and gets refined to the real fixture range at the end of this method.
        var fixtures = await _externalMatchDataService.GetFixturesAsync(
            competition.ExternalApiCompetitionCode,
            competition.StartDate.Year,
            cancellationToken);

        if (fixtures.Count == 0)
        {
            return Result.Fail<FixtureImportSummary>(new ConflictError(
                "The external data source returned no fixtures for this competition and season."));
        }

        var externalTeamCodes = fixtures
            .SelectMany(f => new[] { f.HomeTeamExternalCode, f.AwayTeamExternalCode })
            .Distinct()
            .ToList();

        var teamsByExternalCode = await _dbContext.Team
            .Where(t => t.ExternalApiCode != null && externalTeamCodes.Contains(t.ExternalApiCode))
            .ToDictionaryAsync(t => t.ExternalApiCode!, cancellationToken);

        var unmappedTeamNames = fixtures
            .SelectMany(f => new[] { (Code: f.HomeTeamExternalCode, Name: f.HomeTeamName), (Code: f.AwayTeamExternalCode, Name: f.AwayTeamName) })
            .Where(t => !teamsByExternalCode.ContainsKey(t.Code))
            .Select(t => t.Name)
            .Distinct()
            .ToList();

        if (unmappedTeamNames.Count > 0)
        {
            return Result.Fail<FixtureImportSummary>(new ConflictError(
                $"These teams have no matching Team.ExternalApiCode - map them before importing: {string.Join(", ", unmappedTeamNames)}"));
        }

        var existingTeamCompetitionTeamIds = await _dbContext.TeamCompetition
            .Where(tc => tc.CompetitionID == competitionId)
            .Select(tc => tc.TeamID)
            .ToListAsync(cancellationToken);

        var teamsAdded = 0;
        foreach (var team in teamsByExternalCode.Values.DistinctBy(t => t.TeamID))
        {
            if (existingTeamCompetitionTeamIds.Contains(team.TeamID))
            {
                continue;
            }

            await _dbContext.AddAsync(new TeamCompetition
            {
                TeamCompetitionID = Guid.NewGuid(),
                TeamID = team.TeamID,
                CompetitionID = competitionId,
            }, cancellationToken);

            teamsAdded++;
        }

        var existingExternalMatchIds = await _dbContext.Match
            .Where(m => m.CompetitionID == competitionId && m.ExternalMatchID != null)
            .Select(m => m.ExternalMatchID!.Value)
            .ToListAsync(cancellationToken);

        var matchesImported = 0;
        foreach (var fixture in fixtures)
        {
            if (existingExternalMatchIds.Contains(fixture.ExternalMatchID))
            {
                continue;
            }

            await _dbContext.AddAsync(new Match
            {
                MatchID = Guid.NewGuid(),
                CompetitionID = competitionId,
                MatchDateTime = UkClock.ToUkLocal(fixture.KickoffUtc),
                HomeTeamID = teamsByExternalCode[fixture.HomeTeamExternalCode].TeamID,
                AwayTeamID = teamsByExternalCode[fixture.AwayTeamExternalCode].TeamID,
                ExternalMatchID = fixture.ExternalMatchID,
            }, cancellationToken);

            matchesImported++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var allMatchDates = await _dbContext.Match
            .Where(m => m.CompetitionID == competitionId)
            .Select(m => m.MatchDateTime)
            .ToListAsync(cancellationToken);

        competition.StartDate = DateOnly.FromDateTime(allMatchDates.Min());
        competition.EndDate = DateOnly.FromDateTime(allMatchDates.Max());
        _dbContext.Update(competition);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new FixtureImportSummary
        {
            MatchesImported = matchesImported,
            TeamsAdded = teamsAdded,
            StartDate = competition.StartDate,
            EndDate = competition.EndDate,
        });
    }
}
