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
}
