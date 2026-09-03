using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Predictathon.Application.Attributes;
using Predictathon.Application.Extensions;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using System.Collections.Concurrent;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class LeagueTableService : ILeagueTableService
{
    /// <summary>
    /// How long a computed live table is served to everyone else asking for the same competition.
    /// Shorter than the Live page's 30-second poll, so no viewer is shown a table staler than their
    /// own refresh interval already allows - it only collapses the viewers who happen to poll in
    /// the same window onto one computation.
    /// </summary>
    private static readonly TimeSpan LiveTableLifetime = TimeSpan.FromSeconds(15);

    /// <summary>
    /// One gate per competition, so the first request through recomputes and everybody else waits
    /// for its answer instead of piling a second identical LiveLeagueTableGet onto the database the
    /// moment the entry expires.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> LiveTableGates = new();

    private readonly IGenericDbContext _dbContext;
    private readonly IAvatarService _avatarService;
    private readonly IMemoryCache _cache;

    public LeagueTableService(IGenericDbContext dbContext, IAvatarService avatarService, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _avatarService = avatarService;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeagueTableItem>> GetLeagueTableAsync(
        Guid competitionId,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        DateOnly? dateForComparison = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            new SqlParameter("@DateFrom", SqlDbType.Date) { Value = ToSqlValue(dateFrom) },
            new SqlParameter("@DateTo", SqlDbType.Date) { Value = ToSqlValue(dateTo) },
            new SqlParameter("@DateForComparison", SqlDbType.Date) { Value = ToSqlValue(dateForComparison) },
        };

        var table = await _dbContext.CallStoredProcedureAsync<LeagueTableItem>("LeagueTableGet", parameters, cancellationToken);

        return table.WithAvatarUrls(_avatarService);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Cached briefly, unlike every other read here, because this is the one that gets asked the
    /// same question by many people at once: the answer depends only on the competition - no user
    /// context reaches it - and the Live page re-fetches it every 30 seconds for as long as it is
    /// left open. Twenty people watching a Saturday afternoon otherwise means twenty identical runs
    /// of LiveLeagueTableGet every half minute, which is the heaviest read in the app: every
    /// registered user crossed with every played match, plus a per-prediction scoring pass over the
    /// matches in play.
    ///
    /// Expiry is by time alone, with no invalidation on write. That is deliberate - IIS runs two
    /// worker processes side by side during an overlapped recycle, so an invalidation raised in one
    /// would never reach the other's cache, and a scheme that only sometimes works is worse than one
    /// that visibly always lags by a bounded amount. The cached rows carry avatar URLs too, so a
    /// picture changed mid-match can take up to <see cref="LiveTableLifetime"/> to appear.
    /// </remarks>
    public async Task<IReadOnlyList<LiveLeagueTableItem>> GetLiveLeagueTableAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"LiveLeagueTable:{competitionId}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<LiveLeagueTableItem>? cached) && cached is not null)
        {
            return cached;
        }

        var gate = LiveTableGates.GetOrAdd(competitionId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            // Whoever was holding the gate has just filled the cache, so look again before repeating
            // their work.
            if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            var table = await LoadLiveLeagueTableAsync(competitionId, cancellationToken);
            _cache.Set(cacheKey, table, LiveTableLifetime);

            return table;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Runs the live table for a competition and stamps avatar URLs onto it.
    ///
    /// Kept separate so the cache stores a finished result rather than the in-flight task that
    /// produced it. Sharing the task would tie every waiting request to the first caller's scoped
    /// DbContext, and the Live page abandons its poll whenever someone navigates away - disposing
    /// that scope out from under everyone still awaiting it.
    /// </summary>
    /// <param name="competitionId">The competition whose live table to compute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<IReadOnlyList<LiveLeagueTableItem>> LoadLiveLeagueTableAsync(
        Guid competitionId,
        CancellationToken cancellationToken)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
        };

        // Both the standings and the projected standings come back from one procedure. Ranking the
        // projection here instead would mean writing the tie-break order - points, then goal
        // difference, then 3-, 2- and 1-pointers - a second time in a second language, and two
        // copies of a rule are two rules waiting to disagree.
        var table = await _dbContext.CallStoredProcedureAsync<LiveLeagueTableItem>("LiveLeagueTableGet", parameters, cancellationToken);

        return table.WithAvatarUrls(_avatarService);
    }

    private static object ToSqlValue(DateOnly? date)
        => date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
}
