using FluentResults;
using Mapster;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Common;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class MatchService : CrudService<Guid, CreateMatchModel, MatchModel, Match>, IMatchService
{
    private readonly IGenericDbContext _dbContext;
    private readonly IApplicationDbContext _appDbContext;

    public MatchService(
        ICrudServiceDependencyAggregate<CreateMatchModel, MatchModel> dependencyAggregate,
        IGenericDbContext dbContext,
        IApplicationDbContext appDbContext
    ) : base(dependencyAggregate)
    {
        _dbContext = dbContext;
        _appDbContext = appDbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserMatchPredictionListItem>> GetUserMatchesForWeekAsync(
        Guid userId,
        Guid competitionId,
        DateTime dateFrom,
        CancellationToken cancellationToken = default)
    {
        var dateTo = dateFrom.AddDays(7).AddMilliseconds(-1);

        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            new SqlParameter("@DateFrom", SqlDbType.DateTime) { Value = dateFrom },
            new SqlParameter("@DateTo", SqlDbType.DateTime) { Value = dateTo },
        };

        return await _dbContext.CallStoredProcedureAsync<UserMatchPredictionListItem>("UserMatchPredictionListGet", parameters, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserMatchPredictionListItem>> GetUserPredictionHistoryAsync(
        Guid userId,
        Guid competitionId,
        bool includeFuture,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            new SqlParameter("@HidePastUnpredictedMatches", SqlDbType.Bit) { Value = true },
        };

        if (!includeFuture)
        {
            parameters.Add(new SqlParameter("@DateTo", SqlDbType.DateTime) { Value = UkClock.Now });
        }

        var matches = await _dbContext.CallStoredProcedureAsync<UserMatchPredictionListItem>("UserMatchPredictionListGet", parameters, cancellationToken);

        return matches.OrderByDescending(m => m.MatchDateTime).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MatchModel>> GetForAdminAsync(
        Guid competitionId,
        bool includePlayed,
        CancellationToken cancellationToken = default)
    {
        var matches = await _appDbContext.Match
            .Where(m => m.CompetitionID == competitionId && (includePlayed || !m.MatchPlayed))
            .OrderBy(m => m.MatchDateTime)
            .ToListAsync(cancellationToken);

        return matches.Adapt<List<MatchModel>>();
    }

    private const int ResultEligibleMinutesAfterKickoff = 90;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MatchModel>> GetForProcessingAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        var now = UkClock.Now;

        var matches = await _appDbContext.Match
            .Where(m => m.CompetitionID == competitionId && !m.MatchPlayed && m.MatchDateTime <= now)
            .OrderBy(m => m.MatchDateTime)
            .ToListAsync(cancellationToken);

        return matches.Adapt<List<MatchModel>>();
    }

    /// <inheritdoc />
    public async Task<Result<MatchModel>> SaveResultAsync(
        Guid matchId,
        int homeTeamGoals,
        int awayTeamGoals,
        CancellationToken cancellationToken = default)
    {
        var match = await _appDbContext.Match.FirstOrDefaultAsync(m => m.MatchID == matchId, cancellationToken);
        if (match is null)
        {
            return Result.Fail<MatchModel>(new NotFoundError("The match could not be found."));
        }

        if (UkClock.Now < match.MatchDateTime.AddMinutes(ResultEligibleMinutesAfterKickoff))
        {
            return Result.Fail<MatchModel>(new ConflictError("This match's result can't be entered until 90 minutes after kickoff."));
        }

        match.HomeTeamGoals = homeTeamGoals;
        match.AwayTeamGoals = awayTeamGoals;
        match.MatchPlayed = true;

        _appDbContext.Update(match);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(match.Adapt<MatchModel>());
    }

    /// <summary>
    /// Match.MatchID is ValueGeneratedNever with no database-side default (unlike Competition.CompetitionID,
    /// which defaults to NEWID()), so a new id has to be generated here on create.
    /// </summary>
    protected override Match MapToEntity(CreateMatchModel model)
    {
        var entity = base.MapToEntity(model);
        entity.MatchID = Guid.NewGuid();
        return entity;
    }
}
