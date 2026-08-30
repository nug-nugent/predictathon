using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;

namespace Predictathon.IntegrationTests.TestDoubles;

/// <summary>
/// Stands in for the football-data.org client so tests that exercise the database side of the
/// live-score service never reach out over the network - or spend a call from a real rate-limit
/// budget. The provider's own request/response handling is unit-tested separately.
/// </summary>
public class StubExternalMatchDataService : IExternalMatchDataService
{
    public Task<IReadOnlyList<ExternalFixture>> GetFixturesAsync(string competitionCode, int season, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ExternalFixture>>([]);

    public Task<IReadOnlyList<ExternalMatchScore>> GetScoresAsync(string competitionCode, DateOnly fromUtcDate, DateOnly toUtcDate, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ExternalMatchScore>>([]);
}
