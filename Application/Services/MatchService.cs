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
    private readonly ILeagueDataCache _leagueDataCache;

    public MatchService(
        ICrudServiceDependencyAggregate<CreateMatchModel, MatchModel> dependencyAggregate,
        IGenericDbContext dbContext,
        IApplicationDbContext appDbContext,
        ILeagueDataCache leagueDataCache
    ) : base(dependencyAggregate)
    {
        _dbContext = dbContext;
        _appDbContext = appDbContext;
        _leagueDataCache = leagueDataCache;
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
    public async Task<KnockoutBracketModel> GetKnockoutBracketAsync(
        Guid userId,
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            new SqlParameter("@BracketOnly", SqlDbType.Bit) { Value = true },
        };

        var matches = await _dbContext.CallStoredProcedureAsync<UserMatchPredictionListItem>("UserMatchPredictionListGet", parameters, cancellationToken);

        // The play-off is a knockout match with a round of its own, but it hangs off the semi-finals
        // rather than feeding anything, so it never belongs to a round of the tree.
        var thirdPlacePlayOff = matches.FirstOrDefault(m => m.KnockoutRound == KnockoutRounds.ThirdPlacePlayOffRound);

        var rounds = matches
            .Where(m => m.KnockoutRound.HasValue && m.KnockoutRound != KnockoutRounds.ThirdPlacePlayOffRound)
            .GroupBy(m => m.KnockoutRound!.Value)
            // Descending, so the biggest round - the one the bracket opens with - comes first.
            .OrderByDescending(round => round.Key)
            .Select(round => new KnockoutRoundModel
            {
                KnockoutRound = round.Key,
                RoundName = KnockoutRounds.NameOf(round.Key),
                Matches = round.OrderBy(m => m.BracketSlot ?? int.MaxValue).ThenBy(m => m.MatchDateTime).ToList(),
            })
            .ToList();

        return new KnockoutBracketModel
        {
            Rounds = rounds,
            ThirdPlacePlayOff = thirdPlacePlayOff,
            IsWellFormed = rounds.Count > 0
                && rounds.All(r => r.Matches.All(m => m.BracketSlot.HasValue))
                && KnockoutRounds.IsWellFormedTree([.. rounds.Select(r => (r.KnockoutRound, r.Matches.Count))]),
        };
    }

    /// <inheritdoc />
    public async Task<BracketNumberingSummary> NumberBracketByKickOffAsync(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var bracketMatches = await _appDbContext.Match
            .Where(m => m.CompetitionID == competitionId && m.KnockoutRound != null)
            .ToListAsync(cancellationToken);

        var rounds = bracketMatches.GroupBy(m => m.KnockoutRound!.Value).ToList();

        foreach (var round in rounds)
        {
            // MatchID breaks ties so two fixtures kicking off together still get distinct, stable
            // slots - re-running this on unchanged fixtures has to produce the same numbering, or
            // an admin's corrections would be shuffled by a second press of the button.
            var inKickOffOrder = round.OrderBy(m => m.MatchDateTime).ThenBy(m => m.MatchID).ToList();

            for (var index = 0; index < inKickOffOrder.Count; index++)
            {
                inKickOffOrder[index].BracketSlot = index + 1;
                _appDbContext.Update(inKickOffOrder[index]);
            }
        }

        await _appDbContext.SaveChangesAsync(cancellationToken);

        return new BracketNumberingSummary
        {
            MatchesNumbered = bracketMatches.Count,
            RoundsNumbered = rounds.Count,
        };
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
    public async Task<IReadOnlyList<UserMatchPredictionListItem>> GetLiveDayMatchesAsync(
        Guid userId,
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        var now = UkClock.Now;

        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            new SqlParameter("@DateFrom", SqlDbType.DateTime) { Value = LiveDayWindow.Start(now) },
            new SqlParameter("@DateTo", SqlDbType.DateTime) { Value = LiveDayWindow.End(now) },
        };

        var matches = await _dbContext.CallStoredProcedureAsync<UserMatchPredictionListItem>("UserMatchPredictionListGet", parameters, cancellationToken);

        // The procedure's window is a plain date range, so it can't express "yesterday's matches
        // only while they're still unresolved" - that second half of the rule is applied here.
        return matches
            .Where(m => LiveDayWindow.Includes(m.MatchDateTime, m.MatchPlayed, now))
            .ToList();
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
            .ThenBy(m => m.HomeTeam != null ? m.HomeTeam.TeamName : m.HomeTeamTBC)
            .ToListAsync(cancellationToken);

        return matches.Adapt<List<MatchModel>>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MatchModel>> GetForProcessingAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        // The same window SaveResultAsync enforces: a match that kicked off twenty minutes ago
        // can't have a result saved for it, so offering it here only invites a rejected save. That
        // gap went unnoticed while no sample fixture was ever mid-match; the Today's Matches section
        // put one there.
        var eligibleFrom = MatchResultWindow.EligibleFrom(UkClock.Now);

        var matches = await _appDbContext.Match
            .Where(m => m.CompetitionID == competitionId && !m.MatchPlayed && m.MatchDateTime <= eligibleFrom)
            .OrderBy(m => m.MatchDateTime)
            .ThenBy(m => m.HomeTeam != null ? m.HomeTeam.TeamName : m.HomeTeamTBC)
            .ToListAsync(cancellationToken);

        return matches.Adapt<List<MatchModel>>();
    }

    /// <inheritdoc />
    public async Task<Result<MatchModel>> SaveResultAsync(
        Guid matchId,
        int homeTeamGoals,
        int awayTeamGoals,
        Guid? processedByUserId,
        CancellationToken cancellationToken = default)
    {
        var match = await _appDbContext.Match.FirstOrDefaultAsync(m => m.MatchID == matchId, cancellationToken);
        if (match is null)
        {
            return Result.Fail<MatchModel>(new NotFoundError("The match could not be found."));
        }

        if (!MatchResultWindow.IsEligible(match.MatchDateTime, UkClock.Now))
        {
            return Result.Fail<MatchModel>(new ConflictError(
                $"This match's result can't be entered until {MatchResultWindow.EligibleMinutesAfterKickoff} minutes after kickoff."));
        }

        match.HomeTeamGoals = homeTeamGoals;
        match.AwayTeamGoals = awayTeamGoals;
        match.MatchPlayed = true;

        // Stamped only here, where a result first gets confirmed - a later correction goes through
        // Update and deliberately leaves these alone, so they keep saying how the result arrived
        // rather than who last edited it.
        match.ProcessedDateTime = UkClock.Now;
        match.ProcessedByUserID = processedByUserId;

        _appDbContext.Update(match);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        await RecalculatePredictionScoresAsync(matchId, cancellationToken);

        return Result.Ok(match.Adapt<MatchModel>());
    }

    /// <summary>
    /// Recalculates every prediction's Score and GoalDifference for a match, mirroring the legacy
    /// WebForms app's MatchManager.Save, which called this after every match save so a corrected
    /// score (or a result entered late) always keeps predictions in sync.
    /// </summary>
    public override async Task<Result<MatchModel>> Update(Guid id, MatchModel model, CancellationToken cancellationToken = default)
    {
        var result = await base.Update(id, model, cancellationToken);

        if (result.IsSuccess)
        {
            await RecalculatePredictionScoresAsync(id, cancellationToken);
        }

        return result;
    }

    private async Task RecalculatePredictionScoresAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@MatchID", SqlDbType.UniqueIdentifier) { Value = matchId },
        };

        await _dbContext.CallStoredProcedureAsync("MatchPredictionScoreSet", parameters, cancellationToken);

        // Every route that changes what a match is worth comes through here - a result being
        // entered, and an admin correcting one afterwards - so this is the one place the cached
        // league tables have to be dropped. The competition is looked up rather than passed in for
        // the same reason: it keeps the invalidation attached to the recalculation instead of to
        // each caller remembering to do it.
        var competitionId = await _appDbContext.Match
            .Where(m => m.MatchID == matchId)
            .Select(m => m.CompetitionID)
            .FirstOrDefaultAsync(cancellationToken);

        if (competitionId != Guid.Empty)
        {
            _leagueDataCache.Invalidate(competitionId);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MatchListItem>> GetResultsAsync(
        Guid competitionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
        };

        return await _dbContext.CallStoredProcedureAsync<MatchListItem>("MatchResultListGet", parameters, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MatchListItem?> GetMatchDetailAsync(
        Guid competitionId,
        Guid matchId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            new SqlParameter("@MatchID", SqlDbType.UniqueIdentifier) { Value = matchId },
        };

        var results = await _dbContext.CallStoredProcedureAsync<MatchListItem>("MatchResultListGet", parameters, cancellationToken);

        return results.SingleOrDefault();
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
