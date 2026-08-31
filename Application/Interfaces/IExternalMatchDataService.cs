using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Fetches fixture data from an external match-data provider. Kept provider-agnostic by name so
/// swapping the underlying source (e.g. away from football-data.org) later doesn't ripple through
/// calling code - only the implementation (<c>FootballDataApiClient</c>) knows which provider it is.
/// </summary>
public interface IExternalMatchDataService
{
    /// <summary>
    /// Gets the fixture list for a competition's season from the external data source.
    /// </summary>
    /// <param name="competitionCode">The external provider's competition code (e.g. "PL").</param>
    /// <param name="season">The season's start year (e.g. 2026 for the 2026/27 season).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ExternalFixture>> GetFixturesAsync(string competitionCode, int season, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the scores the provider currently reports for a competition's fixtures over a range of
    /// days - running scores for matches in play, final ones for those already over. A range at once
    /// rather than per match, because calls against the provider are rate limited and every live
    /// fixture in the competition comes back in one response.
    /// </summary>
    /// <param name="competitionCode">The external provider's competition code (e.g. "PL").</param>
    /// <param name="fromUtcDate">First day to fetch, as a UTC date - which is how the provider files fixtures, and isn't always the UK date.</param>
    /// <param name="toUtcDate">Last day to fetch, inclusive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exceptions.ExternalApiRateLimitedException">
    /// The provider's rate limit has been reached, so no request was made.
    /// </exception>
    Task<IReadOnlyList<ExternalMatchScore>> GetScoresAsync(string competitionCode, DateOnly fromUtcDate, DateOnly toUtcDate, CancellationToken cancellationToken = default);
}
