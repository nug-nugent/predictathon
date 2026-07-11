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
}
