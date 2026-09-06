using FluentResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Common;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class PredictionService : IPredictionService
{
    private readonly IApplicationDbContext _appDbContext;

    public PredictionService(IApplicationDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    /// <inheritdoc />
    public async Task<Result> SavePredictionAsync(
        Guid matchId,
        Guid userId,
        int homeTeamGoals,
        int awayTeamGoals,
        CancellationToken cancellationToken = default)
    {
        var match = await _appDbContext.Match.FirstOrDefaultAsync(m => m.MatchID == matchId, cancellationToken);
        if (match is null)
        {
            return Result.Fail(new NotFoundError("The match could not be found."));
        }

        var nowUk = UkClock.Now;
        if (nowUk >= match.MatchDateTime.AddMinutes(-2))
        {
            return Result.Fail(new ConflictError("Predictions can no longer be submitted for this match."));
        }

        var prediction = await _appDbContext.Prediction.FirstOrDefaultAsync(
            p => p.MatchID == matchId && p.UserID == userId, cancellationToken);

        if (prediction is null)
        {
            prediction = new Prediction
            {
                PredictionID = Guid.NewGuid(),
                MatchID = matchId,
                UserID = userId,
            };
            await _appDbContext.AddAsync(prediction, cancellationToken);
        }

        prediction.HomeTeamGoals = homeTeamGoals;
        prediction.AwayTeamGoals = awayTeamGoals;

        // History-first: PredictionHistoryID is an IDENTITY int, only assigned once this row is
        // saved, so the prediction's pointer to it can only be set on a second SaveChanges. This
        // audit trail is what the (separate, not-yet-built) results-processing scoring procedure
        // uses to reconcile late edits - it must be written even though scoring itself is out of
        // scope here, or that feature will silently break later.
        //
        // PredictionDateTime is UK wall-clock (no "Utc" suffix on the column - see CLAUDE.md), which
        // is what MatchPredictionScoreSet's "PredictionHistory.PredictionDateTime <= MatchDateTime"
        // reconciliation compares it against. Writing UtcNow here made every row look an hour early
        // through BST, quietly widening that window.
        var history = new PredictionHistory
        {
            PredictionID = prediction.PredictionID,
            HomeTeamGoals = homeTeamGoals,
            AwayTeamGoals = awayTeamGoals,
            PredictionDateTime = UkClock.Now,
        };
        await _appDbContext.AddAsync(history, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        prediction.PredictionHistoryID = history.PredictionHistoryID;
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MatchPredictionListItem>>> GetMatchPredictionsAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await _appDbContext.Match.FirstOrDefaultAsync(m => m.MatchID == matchId, cancellationToken);
        if (match is null)
        {
            return Result.Fail(new NotFoundError("The match could not be found."));
        }

        // Mirror image of the save cutoff: other users' predictions only become visible once it's
        // no longer possible to submit or change your own, so nobody can use the reveal to copy or
        // second-guess a prediction still in play.
        var nowUk = UkClock.Now;
        if (nowUk < match.MatchDateTime.AddMinutes(-2))
        {
            return Result.Fail(new ConflictError("Other users' predictions aren't visible until 2 minutes before kick-off."));
        }

        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@MatchID", SqlDbType.UniqueIdentifier) { Value = matchId },
        };

        var predictions = await _appDbContext.CallStoredProcedureAsync<MatchPredictionListItem>(
            "MatchPredictionListGet", parameters, cancellationToken);

        return Result.Ok<IReadOnlyList<MatchPredictionListItem>>(predictions);
    }
}
