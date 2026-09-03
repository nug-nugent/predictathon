namespace Predictathon.Application.Interfaces;

/// <summary>
/// Holds computed league tables for a short while and drops them when something makes them wrong.
///
/// The league tables are the only reads in the app worth caching: they're the heaviest queries here
/// (every registered user crossed with every played match), they take no user context at all - so
/// everybody asking about the same competition is asking the identical question - and they're the
/// ones many people request at once, whether by leaving the Live page polling or by all opening the
/// League page after results go in.
///
/// Entries are grouped rather than held individually, because what invalidates them invalidates
/// them together: a processed result changes every variant of a competition's table at once - the
/// full one, each week's, and each comparison date's - and there's no useful way to work out which
/// subset a given result touched.
///
/// Some of what belongs here spans every competition rather than sitting inside one - the all-time
/// league table and the all-time statistics, which aggregate across the lot. Those are held under
/// their own group and dropped by any competition's invalidation, since a result anywhere changes
/// what they say.
/// </summary>
public interface ILeagueDataCache
{
    /// <summary>
    /// Returns the cached value for a key, computing and storing it if it isn't there. Concurrent
    /// callers asking for the same key wait for the first one's answer rather than each running the
    /// query.
    /// </summary>
    /// <typeparam name="T">The cached value's type.</typeparam>
    /// <param name="competitionId">The competition the entry belongs to, for invalidation.</param>
    /// <param name="key">Identifies this particular table within the competition.</param>
    /// <param name="factory">Computes the value when it isn't already cached.</param>
    /// <param name="lifetime">How long the computed value stays usable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T> GetOrCreateAsync<T>(
        Guid competitionId,
        string key,
        Func<Task<T>> factory,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Returns the cached value for a key that spans every competition, computing and storing it if
    /// it isn't there. Same contract as <see cref="GetOrCreateAsync"/> otherwise.
    /// </summary>
    /// <typeparam name="T">The cached value's type.</typeparam>
    /// <param name="key">Identifies this particular aggregate.</param>
    /// <param name="factory">Computes the value when it isn't already cached.</param>
    /// <param name="lifetime">How long the computed value stays usable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T> GetOrCreateAllTimeAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Drops every cached table for a competition, and every all-time aggregate along with it -
    /// a result in any competition changes what the all-time tables say. Call this after anything that changes what its
    /// league table would say - a result being processed, or somebody joining.
    /// </summary>
    /// <param name="competitionId">The competition whose tables are now out of date.</param>
    void Invalidate(Guid competitionId);
}
