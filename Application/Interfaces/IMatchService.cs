using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IMatchService
{
    /// <summary>
    /// Gets the matches in the 7-day week starting at <paramref name="dateFrom"/> for a competition,
    /// each joined with the given user's own prediction for it (if any).
    /// </summary>
    /// <param name="userId">The user whose predictions to include.</param>
    /// <param name="competitionId">The competition to get matches for.</param>
    /// <param name="dateFrom">The first day of the week to get matches for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<UserMatchPredictionListItem>> GetUserMatchesForWeekAsync(
        Guid userId,
        Guid competitionId,
        DateTime dateFrom,
        CancellationToken cancellationToken = default);
}
