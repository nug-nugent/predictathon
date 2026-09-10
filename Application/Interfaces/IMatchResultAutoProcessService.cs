using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Turns a settled live score into a confirmed result, so an admin doesn't have to copy a full-time
/// score the provider has already told us about onto the Process Results page.
///
/// Deliberately separate from <see cref="ILiveScoreService"/>, which stays about provisional scores:
/// this reads only what that has already stored and makes no call to the provider, so it costs a
/// query and none of the rate-limit budget, and can run on every poll pass whether or not anything
/// was in play.
/// </summary>
public interface IMatchResultAutoProcessService
{
    /// <summary>
    /// Confirms the result of every match the provider has called finished and that nobody has
    /// processed yet, scoring its predictions as if an admin had entered the score by hand. Safe to
    /// call at any time and as often as you like: a match already processed is left alone, so a
    /// second sweep over the same matches does nothing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ResultAutoProcessSummary> ProcessFinishedMatchesAsync(CancellationToken cancellationToken = default);
}
