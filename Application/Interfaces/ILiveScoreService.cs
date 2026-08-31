using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Keeps provisional in-play scores up to date for matches that have kicked off but have no
/// confirmed result yet. All the actual work lives here rather than in the hosted service that
/// drives it, so it can be tested without a host and triggered from anywhere else later (an
/// external scheduler hitting TasksController, say) without moving any logic.
/// </summary>
public interface ILiveScoreService
{
    /// <summary>
    /// Fetches scores from the external provider for every match currently in play and stores what
    /// it learns. Safe to call at any time and as often as the rate limit allows: with nothing in
    /// play it makes no provider call at all.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<LiveScoreRefreshSummary> RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// How long to wait before the next <see cref="RefreshAsync"/>: the configured poll interval
    /// while anything is in play, otherwise the time until the next fixture kicks off. This is what
    /// keeps the poller quiet - on a day with no football it asks the database once an hour and
    /// makes no provider calls at all.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TimeSpan> GetNextRefreshDelayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a score entered by a match administrator, overwriting whatever the provider last
    /// reported. Unlike the provider's own updates this may lower a score, which is the only way to
    /// correct a goal the feed reported and a VAR review then chalked off.
    /// </summary>
    /// <param name="matchId">The match to score.</param>
    /// <param name="homeTeamGoals">Home goals so far.</param>
    /// <param name="awayTeamGoals">Away goals so far.</param>
    /// <param name="userId">The administrator making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<MatchLiveScoreModel>> SaveAdminScoreAsync(
        Guid matchId,
        int homeTeamGoals,
        int awayTeamGoals,
        Guid userId,
        CancellationToken cancellationToken = default);
}
