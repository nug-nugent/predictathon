using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface ILeagueTableService
{
    /// <summary>
    /// Gets the league table for a competition, optionally restricted to matches within a date
    /// range, and optionally including each user's previous position as of a comparison date.
    /// </summary>
    /// <param name="competitionId">The competition to get the table for.</param>
    /// <param name="dateFrom">Only include matches played on or after this date, if supplied.</param>
    /// <param name="dateTo">Only include matches played on or before this date, if supplied.</param>
    /// <param name="dateForComparison">If supplied, populates <see cref="LeagueTableItem.PreviousLeaguePosition"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<LeagueTableItem>> GetLeagueTableAsync(
        Guid competitionId,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        DateOnly? dateForComparison = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the competition's league table as it stands, with each row carrying what the matches
    /// currently in play are worth to that user so far - the Live page's table. The standings
    /// themselves are unchanged and stay in their real order: the live points are what's *not* in
    /// them yet, and sorting by a projection would make the table reshuffle under a reader watching
    /// a match.
    /// </summary>
    /// <param name="competitionId">The competition to get the table for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <summary>
    /// The confirmed league table, computed fresh rather than served from the cache.
    ///
    /// For the one caller that doesn't just display the table but turns it into a permanent record:
    /// Hall of Fame generation, which reads off the top three and writes them down for good. The
    /// cached read is invalidated by everything that would change the answer, so in the ordinary
    /// case this returns the same rows - but "ordinary case" is the wrong standard for a result
    /// that can't be taken back, and an overlapped IIS recycle can leave a second worker holding a
    /// table an invalidation never reached.
    /// </summary>
    /// <param name="competitionId">The competition whose table to compute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<LeagueTableItem>> GetLeagueTableUncachedAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LiveLeagueTableItem>> GetLiveLeagueTableAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);
}
