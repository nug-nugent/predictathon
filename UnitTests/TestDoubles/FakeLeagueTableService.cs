using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;

namespace Predictathon.UnitTests.TestDoubles;

/// <summary>
/// Stand-in for <see cref="ILeagueTableService"/> that returns a fixed, pre-arranged table rather
/// than computing one, since the real implementation calls the LeagueTableGet stored procedure and
/// isn't exercisable against the InMemory EF provider.
/// </summary>
public class FakeLeagueTableService : ILeagueTableService
{
    public IReadOnlyList<LeagueTableItem> Table { get; set; } = [];

    public Task<IReadOnlyList<LeagueTableItem>> GetLeagueTableAsync(
        Guid competitionId,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        DateOnly? dateForComparison = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Table);

    /// <summary>Whether the uncached read was the one used - see HallOfFameServiceTests.</summary>
    public bool UncachedTableRequested { get; private set; }

    public Task<IReadOnlyList<LeagueTableItem>> GetLeagueTableUncachedAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        UncachedTableRequested = true;
        return Task.FromResult(Table);
    }

    public IReadOnlyList<LiveLeagueTableItem> LiveTable { get; set; } = [];

    public Task<IReadOnlyList<LiveLeagueTableItem>> GetLiveLeagueTableAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(LiveTable);
}
