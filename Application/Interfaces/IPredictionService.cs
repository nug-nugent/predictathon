using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IPredictionService
{
    /// <summary>
    /// Upserts the given user's prediction for a match. Rejected with a <see cref="Errors.ConflictError"/>
    /// once the match is within 2 minutes of kick-off - this is the authoritative cutoff check; any
    /// client-side prediction window is only a UX convenience and must not be trusted on its own.
    /// </summary>
    /// <param name="matchId">The match being predicted.</param>
    /// <param name="userId">The user submitting the prediction.</param>
    /// <param name="homeTeamGoals">Predicted home team goals.</param>
    /// <param name="awayTeamGoals">Predicted away team goals.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result> SavePredictionAsync(
        Guid matchId,
        Guid userId,
        int homeTeamGoals,
        int awayTeamGoals,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every registered competitor's prediction for a match. Rejected with a
    /// <see cref="Errors.ConflictError"/> until the match is within 2 minutes of kick-off - the
    /// mirror image of <see cref="SavePredictionAsync"/>'s cutoff.
    /// </summary>
    /// <param name="matchId">The match to get predictions for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<IReadOnlyList<MatchPredictionListItem>>> GetMatchPredictionsAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);
}
