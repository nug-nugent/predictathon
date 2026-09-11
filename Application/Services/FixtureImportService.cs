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

        // Undecided sides are dropped from both the lookup and the mapping check below. A
        // tournament publishes its knockout schedule long before the draw, so those fixtures arrive
        // with no team on either side - and the check refused the whole season over them, naming a
        // blank team as unmapped. Nothing could import a bracket until its last group game was done.
        var externalTeamCodes = fixtures
            .SelectMany(f => new[] { f.HomeTeamExternalCode, f.AwayTeamExternalCode })
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .ToList();

        var teamsByExternalCode = await _dbContext.Team
            .Where(t => t.ExternalApiCode != null && externalTeamCodes.Contains(t.ExternalApiCode))
            .ToDictionaryAsync(t => t.ExternalApiCode!, cancellationToken);

        // A named team we can't match is still a hard stop: it means our Team catalogue is missing
        // an ExternalApiCode, and importing around it would silently leave that team's fixtures out.
        var unmappedTeamNames = fixtures
            .SelectMany(f => new[] { (Code: f.HomeTeamExternalCode, Name: f.HomeTeamName), (Code: f.AwayTeamExternalCode, Name: f.AwayTeamName) })
            .Where(t => !string.IsNullOrWhiteSpace(t.Code) && !teamsByExternalCode.ContainsKey(t.Code))
            .Select(t => t.Name)
            .Distinct()
            .ToList();

        if (unmappedTeamNames.Count > 0)
        {
            return Result.Fail<FixtureImportSummary>(new ConflictError(
                $"These teams have no matching Team.ExternalApiCode - map them before importing: {string.Join(", ", unmappedTeamNames)}"));
        }

        var groupNameByTeamId = BuildGroupNameByTeamId(fixtures, teamsByExternalCode);

        var existingTeamCompetitions = await _dbContext.TeamCompetition
            .Where(tc => tc.CompetitionID == competitionId)
            .ToListAsync(cancellationToken);

        var existingTeamCompetitionTeamIds = existingTeamCompetitions.Select(tc => tc.TeamID).ToHashSet();

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
                GroupName = groupNameByTeamId.GetValueOrDefault(team.TeamID),
            }, cancellationToken);

            teamsAdded++;
        }

        // Teams already assigned - added by hand before the import, or by an earlier run - get their
        // group filled in too, since the draw is usually published after the teams are known. An
        // existing group is left alone: it may be an admin's correction, and the provider shouldn't
        // overwrite that on every sync.
        var groupsAssigned = 0;
        foreach (var teamCompetition in existingTeamCompetitions)
        {
            if (!string.IsNullOrWhiteSpace(teamCompetition.GroupName))
            {
                continue;
            }

            if (!groupNameByTeamId.TryGetValue(teamCompetition.TeamID, out var groupName))
            {
                continue;
            }

            teamCompetition.GroupName = groupName;
            _dbContext.Update(teamCompetition);
            groupsAssigned++;
        }

        var existingExternalMatchIds = await _dbContext.Match
            .Where(m => m.CompetitionID == competitionId && m.ExternalMatchID != null)
            .Select(m => m.ExternalMatchID!.Value)
            .ToListAsync(cancellationToken);

        var matchesImported = 0;
        var matchesAwaitingTeams = 0;
        foreach (var fixture in fixtures)
        {
            if (existingExternalMatchIds.Contains(fixture.ExternalMatchID))
            {
                continue;
            }

            var homeTeam = TeamOrNull(teamsByExternalCode, fixture.HomeTeamExternalCode);
            var awayTeam = TeamOrNull(teamsByExternalCode, fixture.AwayTeamExternalCode);

            await _dbContext.AddAsync(new Match
            {
                MatchID = Guid.NewGuid(),
                CompetitionID = competitionId,
                MatchDateTime = UkClock.ToUkLocal(fixture.KickoffUtc),
                HomeTeamID = homeTeam?.TeamID,
                AwayTeamID = awayTeam?.TeamID,
                // Where the provider names an undecided side ("Winner Group A"), that name is the
                // placeholder. Where it says nothing, the slot stays blank for an admin to fill in -
                // better an empty slot than a made-up one, since only the draw knows what feeds it.
                HomeTeamTBC = homeTeam is null ? NullIfBlank(fixture.HomeTeamName) : null,
                AwayTeamTBC = awayTeam is null ? NullIfBlank(fixture.AwayTeamName) : null,
                ExternalMatchID = fixture.ExternalMatchID,
                Description = fixture.Description,
                Knockout = fixture.IsKnockout,
                KnockoutRound = fixture.KnockoutRound,
            }, cancellationToken);

            matchesImported++;

            if (homeTeam is null || awayTeam is null)
            {
                matchesAwaitingTeams++;
            }
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
            MatchesAwaitingTeams = matchesAwaitingTeams,
            TeamsAdded = teamsAdded,
            GroupsAssigned = groupsAssigned,
            StartDate = competition.StartDate,
            EndDate = competition.EndDate,
        });
    }

    /// <summary>The team a provider code maps to, or null where the code is blank or unknown.</summary>
    /// <param name="teamsByExternalCode">The site's teams, keyed by the provider's team code.</param>
    /// <param name="externalCode">The provider's team code, possibly empty.</param>
    private static Team? TeamOrNull(Dictionary<string, Team> teamsByExternalCode, string externalCode)
        => string.IsNullOrWhiteSpace(externalCode) ? null : teamsByExternalCode.GetValueOrDefault(externalCode);

    /// <summary>The text, or null where it is blank - the shape the TBC columns want.</summary>
    /// <param name="text">The text to normalise.</param>
    private static string? NullIfBlank(string text) => string.IsNullOrWhiteSpace(text) ? null : text;

    /// <summary>
    /// Works out which group each team is drawn into, from the groups its fixtures are played in.
    /// A team with fixtures in more than one group - which shouldn't happen, but would leave the
    /// group tables incoherent if it did - is left ungrouped rather than arbitrarily assigned.
    /// </summary>
    /// <param name="fixtures">The fixtures reported by the external data source.</param>
    /// <param name="teamsByExternalCode">The site's teams, keyed by the provider's team code.</param>
    private static Dictionary<Guid, string> BuildGroupNameByTeamId(
        IReadOnlyList<ExternalFixture> fixtures,
        Dictionary<string, Team> teamsByExternalCode)
    {
        var groupNamesByTeamId = new Dictionary<Guid, HashSet<string>>();

        foreach (var fixture in fixtures.Where(f => !string.IsNullOrWhiteSpace(f.GroupName)))
        {
            foreach (var externalCode in new[] { fixture.HomeTeamExternalCode, fixture.AwayTeamExternalCode })
            {
                if (!teamsByExternalCode.TryGetValue(externalCode, out var team))
                {
                    continue;
                }

                if (!groupNamesByTeamId.TryGetValue(team.TeamID, out var groupNames))
                {
                    groupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    groupNamesByTeamId[team.TeamID] = groupNames;
                }

                groupNames.Add(fixture.GroupName!.Trim());
            }
        }

        return groupNamesByTeamId
            .Where(pair => pair.Value.Count == 1)
            .ToDictionary(pair => pair.Key, pair => pair.Value.First());
    }
}
