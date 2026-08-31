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

    public IReadOnlyList<ExternalMatchScore> Scores { get; set; } = [];

    /// <summary>What GetScoresAsync has been asked for, in call order.</summary>
    public List<(string CompetitionCode, DateOnly FromUtcDate, DateOnly ToUtcDate)> ScoreRequests { get; } = [];

    public Task<IReadOnlyList<ExternalFixture>> GetFixturesAsync(string competitionCode, int season, CancellationToken cancellationToken = default)
        => Task.FromResult(Fixtures);

    public Task<IReadOnlyList<ExternalMatchScore>> GetScoresAsync(string competitionCode, DateOnly fromUtcDate, DateOnly toUtcDate, CancellationToken cancellationToken = default)
    {
        ScoreRequests.Add((competitionCode, fromUtcDate, toUtcDate));
        return Task.FromResult(Scores);
    }
}
