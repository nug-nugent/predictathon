using FluentAssertions;
using Predictathon.Application.Services;

namespace Predictathon.UnitTests.Services;

/// <summary>
/// Covers the league table cache's two jobs: not running the same query twice while an entry is
/// good, and letting go of it the moment something makes it wrong. The second is the one worth
/// testing - a cache that never invalidates still looks fast, and shows everyone last week's table.
/// </summary>
public class LeagueTableCacheTests
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task GetOrCreateAsync_RunsTheQueryOnce_AndReusesTheResult()
    {
        var cache = new LeagueTableCache();
        var competitionId = Guid.NewGuid();
        var calls = 0;

        Task<string> Factory()
        {
            calls++;
            return Task.FromResult("table");
        }

        var first = await cache.GetOrCreateAsync(competitionId, "key", Factory, Lifetime);
        var second = await cache.GetOrCreateAsync(competitionId, "key", Factory, Lifetime);

        first.Should().Be("table");
        second.Should().Be("table");
        calls.Should().Be(1);
    }

    [Fact]
    public async Task Invalidate_MakesTheNextRequestRecompute()
    {
        var cache = new LeagueTableCache();
        var competitionId = Guid.NewGuid();
        var calls = 0;

        Task<string> Factory()
        {
            calls++;
            return Task.FromResult($"table {calls}");
        }

        await cache.GetOrCreateAsync(competitionId, "key", Factory, Lifetime);
        cache.Invalidate(competitionId);
        var afterInvalidation = await cache.GetOrCreateAsync(competitionId, "key", Factory, Lifetime);

        calls.Should().Be(2);
        afterInvalidation.Should().Be("table 2");
    }

    [Fact]
    public async Task Invalidate_DropsEveryVariantOfTheCompetitionsTable()
    {
        // A processed result changes the full table, each week's filtered table and each comparison
        // date's - so invalidating has to take them all, not just the one that happens to be keyed
        // on no dates at all.
        var cache = new LeagueTableCache();
        var competitionId = Guid.NewGuid();
        var calls = 0;

        Task<string> Factory()
        {
            calls++;
            return Task.FromResult("table");
        }

        await cache.GetOrCreateAsync(competitionId, "table:all", Factory, Lifetime);
        await cache.GetOrCreateAsync(competitionId, "table:thisweek", Factory, Lifetime);
        await cache.GetOrCreateAsync(competitionId, "live", Factory, Lifetime);
        calls.Should().Be(3);

        cache.Invalidate(competitionId);

        await cache.GetOrCreateAsync(competitionId, "table:all", Factory, Lifetime);
        await cache.GetOrCreateAsync(competitionId, "table:thisweek", Factory, Lifetime);
        await cache.GetOrCreateAsync(competitionId, "live", Factory, Lifetime);

        calls.Should().Be(6);
    }

    [Fact]
    public async Task Invalidate_LeavesOtherCompetitionsAlone()
    {
        var cache = new LeagueTableCache();
        var invalidated = Guid.NewGuid();
        var untouched = Guid.NewGuid();
        var calls = 0;

        Task<string> Factory()
        {
            calls++;
            return Task.FromResult("table");
        }

        await cache.GetOrCreateAsync(invalidated, "a", Factory, Lifetime);
        await cache.GetOrCreateAsync(untouched, "b", Factory, Lifetime);
        calls.Should().Be(2);

        cache.Invalidate(invalidated);

        await cache.GetOrCreateAsync(untouched, "b", Factory, Lifetime);

        calls.Should().Be(2);
    }

    [Fact]
    public async Task GetOrCreateAsync_CollapsesConcurrentCallersOntoOneQuery()
    {
        // The reason the gate exists: the Live page has every viewer polling on their own timer, so
        // an expired entry is asked for by several at once. Without it they'd each run the heaviest
        // query in the app rather than waiting for the first one's answer.
        var cache = new LeagueTableCache();
        var competitionId = Guid.NewGuid();
        var calls = 0;
        var release = new TaskCompletionSource();

        async Task<string> Factory()
        {
            Interlocked.Increment(ref calls);
            await release.Task;
            return "table";
        }

        var concurrent = Enumerable.Range(0, 10)
            .Select(_ => cache.GetOrCreateAsync(competitionId, "key", Factory, Lifetime))
            .ToList();

        release.SetResult();
        var results = await Task.WhenAll(concurrent);

        results.Should().AllBe("table");
        calls.Should().Be(1);
    }

    [Fact]
    public async Task Invalidate_DuringAQuery_DoesNotLeaveTheStaleResultCached()
    {
        // The order that matters: if the entry were registered against an invalidation token taken
        // before the query ran, an invalidation raised while it was running would be spent on
        // nothing and the result it was meant to discard would be cached anyway.
        var cache = new LeagueTableCache();
        var competitionId = Guid.NewGuid();
        var calls = 0;
        var started = new TaskCompletionSource();
        var release = new TaskCompletionSource();

        async Task<string> Factory()
        {
            var call = Interlocked.Increment(ref calls);
            if (call == 1)
            {
                started.SetResult();
                await release.Task;
            }

            return $"table {call}";
        }

        var inFlight = cache.GetOrCreateAsync(competitionId, "key", Factory, Lifetime);
        await started.Task;

        cache.Invalidate(competitionId);
        release.SetResult();
        await inFlight;

        var next = await cache.GetOrCreateAsync(competitionId, "key", Factory, Lifetime);

        calls.Should().Be(2);
        next.Should().Be("table 2");
    }
}
