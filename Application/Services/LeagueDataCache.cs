using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Predictathon.Application.Interfaces;
using System.Collections.Concurrent;

namespace Predictathon.Application.Services;

/// <summary>
/// The <see cref="ILeagueDataCache"/> implementation. Registered as a singleton in Program.cs
/// rather than picked up by the [ScopedService] scan - a per-request cache would start empty on
/// every request and cache nothing at all.
/// </summary>
public sealed class LeagueDataCache : ILeagueDataCache
{
    /// <summary>
    /// A ceiling on how many tables are held at once. The date parameters that form part of a key
    /// come from the query string, so without a bound an authenticated caller could mint an
    /// unlimited number of distinct keys - each holding a full league table - just by walking
    /// dateFrom through a range. In ordinary use a competition needs a handful of entries, so this
    /// is far above what the site asks for and far below what would trouble the worker process.
    /// </summary>
    private const int MaximumEntries = 200;

    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = MaximumEntries });

    /// <summary>
    /// One token source per competition, so cancelling it evicts every entry for that competition
    /// in one go. MemoryCache can't enumerate or remove by key prefix, and an entry's expiration
    /// token is the supported way to tie a group of entries to a single signal.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _invalidationTokens = new();

    /// <summary>
    /// The equivalent for entries that span every competition. Held as one field rather than in the
    /// dictionary above because there's only ever one such group, and every invalidation clears it.
    /// </summary>
    private CancellationTokenSource _allTimeInvalidationToken = new();

    /// <summary>
    /// One gate per key, so that when an entry expires the first caller recomputes it and the rest
    /// wait for that answer instead of piling identical queries onto the database together.
    /// </summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(
        Guid competitionId,
        string key,
        Func<Task<T>> factory,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            // Whoever was holding the gate has just filled the cache, so look again before repeating
            // their work.
            if (_cache.TryGetValue(key, out cached) && cached is not null)
            {
                return cached;
            }

            // Taken before the query runs, not after. An invalidation raised while it was running
            // has to win, and it can only do that if the token it cancels is the one this entry
            // ends up registered against - claim it afterwards and the invalidation lands on a
            // token nobody holds, leaving the result it was meant to discard cached anyway. Setting
            // an entry against an already-cancelled token is what makes this work: it expires on
            // arrival rather than being stored.
            var invalidationToken = _invalidationTokens.GetOrAdd(competitionId, _ => new CancellationTokenSource());

            var value = await factory();

            _cache.Set(key, value, BuildOptions(lifetime, invalidationToken));

            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrCreateAllTimeAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            if (_cache.TryGetValue(key, out cached) && cached is not null)
            {
                return cached;
            }

            // Read before the query runs, for the same reason as the per-competition path above:
            // an invalidation raised while it's running has to land on the token this entry ends up
            // registered against.
            var invalidationToken = Volatile.Read(ref _allTimeInvalidationToken);

            var value = await factory();

            _cache.Set(key, value, BuildOptions(lifetime, invalidationToken));

            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public void Invalidate(Guid competitionId)
    {
        // The all-time aggregates go first and unconditionally: they're affected by a result in any
        // competition, including one that has nothing cached of its own.
        var previousAllTime = Interlocked.Exchange(ref _allTimeInvalidationToken, new CancellationTokenSource());
        previousAllTime.Cancel();

        if (_invalidationTokens.TryRemove(competitionId, out var tokenSource))
        {
            // Cancelled but deliberately not disposed. A caller in GetOrCreateAsync may be holding
            // this same source and about to read its Token, which throws once it's disposed - and
            // an invalidation happens a few times a day, so letting the collector take these is a
            // better trade than a lock on the read path.
            tokenSource.Cancel();
        }
    }

    /// <summary>
    /// The entry options every cached value shares: a lifetime, a unit of the size budget, and the
    /// token that lets its whole group be dropped at once.
    /// </summary>
    /// <param name="lifetime">How long the value stays usable.</param>
    /// <param name="invalidationToken">The group this entry is dropped with.</param>
    private static MemoryCacheEntryOptions BuildOptions(TimeSpan lifetime, CancellationTokenSource invalidationToken)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lifetime,
            Size = 1,
        };
        options.AddExpirationToken(new CancellationChangeToken(invalidationToken.Token));

        return options;
    }
}
