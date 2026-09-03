using Microsoft.Data.SqlClient;
using Predictathon.Application.Attributes;
using Predictathon.Application.Extensions;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using System.Data;
using System.Globalization;

namespace Predictathon.Application.Services;

[ScopedService]
public class LeagueTableService : ILeagueTableService
{
    /// <summary>
    /// How long a confirmed league table is reused for. Short, but the cache doesn't lean on it:
    /// the things that change this table - a result being processed, somebody joining - invalidate
    /// it outright, so this is a backstop for whatever that misses. It matters most during an
    /// overlapped IIS recycle, when two workers run side by side and an invalidation raised in one
    /// never reaches the other's cache.
    /// </summary>
    private static readonly TimeSpan TableLifetime = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How long a live table is reused for. Shorter than the confirmed one and expiring on time
    /// alone, because what changes it - a goal arriving from the provider's feed - isn't a write
    /// anybody can hang an invalidation off. Shorter than the Live page's own 30-second poll, so no
    /// viewer sees a table staler than their refresh interval already allows.
    /// </summary>
    private static readonly TimeSpan LiveTableLifetime = TimeSpan.FromSeconds(15);

    private readonly IGenericDbContext _dbContext;
    private readonly IAvatarService _avatarService;
    private readonly ILeagueTableCache _cache;

    public LeagueTableService(IGenericDbContext dbContext, IAvatarService avatarService, ILeagueTableCache cache)
    {
        _dbContext = dbContext;
        _avatarService = avatarService;
        _cache = cache;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Cached per competition and date range. Nothing about the caller reaches this query, so every
    /// viewer of a competition's League page - and every Home page's mini table, which asks with
    /// today's date as the comparison - is asking the identical question. Results going in is
    /// exactly when everyone looks at once, and that's also the moment the entry is thrown away, so
    /// the burst that follows a processed result is served from one computation rather than fifty.
    /// </remarks>
    public async Task<IReadOnlyList<LeagueTableItem>> GetLeagueTableAsync(
        Guid competitionId,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        DateOnly? dateForComparison = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"table:{competitionId}:{Stamp(dateFrom)}:{Stamp(dateTo)}:{Stamp(dateForComparison)}";

        return await _cache.GetOrCreateAsync(
            competitionId,
            key,
            () => LoadLeagueTableAsync(competitionId, dateFrom, dateTo, dateForComparison, cancellationToken),
            TableLifetime,
            cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Cached like the confirmed table above, and for a stronger reason: this is the heaviest read
    /// in the app - every registered user crossed with every played match, plus a per-prediction
    /// scoring pass over whatever is in play - and the Live page re-fetches it every 30 seconds for
    /// as long as it's left open. Twenty people watching a Saturday afternoon would otherwise mean
    /// twenty identical runs of it every half minute.
    ///
    /// The cached rows carry avatar URLs, so a picture changed mid-match can take until the entry
    /// expires to appear.
    /// </remarks>
    public async Task<IReadOnlyList<LiveLeagueTableItem>> GetLiveLeagueTableAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            competitionId,
            $"live:{competitionId}",
            () => LoadLiveLeagueTableAsync(competitionId, cancellationToken),
            LiveTableLifetime,
            cancellationToken);
    }

    /// <summary>
    /// Runs the confirmed league table and stamps avatar URLs onto it.
    /// </summary>
    /// <param name="competitionId">The competition whose table to compute.</param>
    /// <param name="dateFrom">Only include matches played on or after this date.</param>
    /// <param name="dateTo">Only include matches played on or before this date.</param>
    /// <param name="dateForComparison">If supplied, each row's previous position as of this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<IReadOnlyList<LeagueTableItem>> LoadLeagueTableAsync(
        Guid competitionId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        DateOnly? dateForComparison,
        CancellationToken cancellationToken)
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

    /// <summary>
    /// Runs the live league table and stamps avatar URLs onto it.
    ///
    /// Kept separate from the cache lookup so what's stored is a finished result rather than the
    /// in-flight task that produced it. Sharing the task would tie every waiting request to the
    /// first caller's scoped DbContext, and the Live page abandons its poll whenever somebody
    /// navigates away - disposing that scope out from under everyone still awaiting it.
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

    /// <summary>
    /// Renders an optional date for use in a cache key, so that "no date" and a real one can't
    /// collide.
    /// </summary>
    /// <param name="date">The date to render.</param>
    private static string Stamp(DateOnly? date)
        => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";

    private static object ToSqlValue(DateOnly? date)
        => date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
}
