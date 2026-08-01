using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;

namespace Predictathon.UnitTests.TestDoubles;

/// <summary>
/// Stand-in for <see cref="IExternalMatchDataService"/> that returns a fixed, pre-arranged fixture
/// list rather than calling a real external API.
/// </summary>
public class FakeExternalMatchDataService : IExternalMatchDataService
{
    public IReadOnlyList<ExternalFixture> Fixtures { get; set; } = [];

    public Task<IReadOnlyList<ExternalFixture>> GetFixturesAsync(string competitionCode, int season, CancellationToken cancellationToken = default)
        => Task.FromResult(Fixtures);
}
