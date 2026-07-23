using FluentResults;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Services;

[ScopedService]
public class HallOfFameService : IHallOfFameService
{
    private readonly IGenericDbContext _dbContext;
    private readonly IApplicationDbContext _appDbContext;
    private readonly ILeagueTableService _leagueTableService;

    public HallOfFameService(IGenericDbContext dbContext, IApplicationDbContext appDbContext, ILeagueTableService leagueTableService)
    {
        _dbContext = dbContext;
        _appDbContext = appDbContext;
        _leagueTableService = leagueTableService;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HallOfFameListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CallStoredProcedureAsync<HallOfFameListItem>("HallOfFameListGet", cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<HallOfFameGenerationStatus> GetGenerationStatusAsync(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var matches = await _appDbContext.Match
            .Where(m => m.CompetitionID == competitionId)
            .Select(m => m.MatchPlayed)
            .ToListAsync(cancellationToken);

        var alreadyGenerated = await _appDbContext.HallOfFame
            .AnyAsync(h => h.CompetitionID == competitionId, cancellationToken);

        return new HallOfFameGenerationStatus
        {
            AllMatchesPlayed = matches.Count > 0 && matches.All(played => played),
            AlreadyGenerated = alreadyGenerated,
        };
    }

    /// <inheritdoc />
    public async Task<Result<HallOfFameListItem>> GenerateForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var competition = await _appDbContext.Competition
            .FirstOrDefaultAsync(c => c.CompetitionID == competitionId, cancellationToken);
        if (competition is null)
        {
            return Result.Fail(new NotFoundError("Competition not found."));
        }

        var alreadyGenerated = await _appDbContext.HallOfFame
            .AnyAsync(h => h.CompetitionID == competitionId, cancellationToken);
        if (alreadyGenerated)
        {
            return Result.Fail(new ConflictError("This competition already has a Hall of Fame entry."));
        }

        var matches = await _appDbContext.Match
            .Where(m => m.CompetitionID == competitionId)
            .Select(m => m.MatchPlayed)
            .ToListAsync(cancellationToken);
        if (matches.Count == 0 || !matches.All(played => played))
        {
            return Result.Fail(new ConflictError("Not all matches in this competition have been played yet."));
        }

        var leagueTable = await _leagueTableService.GetLeagueTableAsync(competitionId, cancellationToken: cancellationToken);
        if (leagueTable.Count < 3)
        {
            return Result.Fail(new ConflictError("At least 3 users need a league position before Hall of Fame entries can be generated."));
        }

        var entity = new HallOfFame
        {
            HallOfFameID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            CompetitionName = competition.CompetitionName,
            Winner = leagueTable[0].Username,
            WinnerUserID = leagueTable[0].UserID,
            SecondPlace = leagueTable[1].Username,
            SecondPlaceUserID = leagueTable[1].UserID,
            ThirdPlace = leagueTable[2].Username,
            ThirdPlaceUserID = leagueTable[2].UserID,
            EndDate = competition.EndDate,
            ImageFilename = competition.ImageFilename,
        };

        await _appDbContext.AddAsync(entity, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new HallOfFameListItem
        {
            HallOfFameID = entity.HallOfFameID,
            CompetitionID = entity.CompetitionID,
            CompetitionName = entity.CompetitionName,
            EndDate = entity.EndDate,
            ImageFilename = entity.ImageFilename,
            Winner = entity.Winner,
            WinnerUserID = entity.WinnerUserID,
            SecondPlace = entity.SecondPlace,
            SecondPlaceUserID = entity.SecondPlaceUserID,
            ThirdPlace = entity.ThirdPlace,
            ThirdPlaceUserID = entity.ThirdPlaceUserID,
        });
    }
}
