using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Interfaces;

// TCreateModel: CreateMatchModel (client-supplied fields)
// TEditModel: MatchModel (full model including MatchID)
public interface IMatchService : ICrudService<Guid, CreateMatchModel, MatchModel, Match>
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

    /// <summary>
    /// Gets every match for a competition for admin management, optionally excluding already-played
    /// matches, ordered by date.
    /// </summary>
    Task<IReadOnlyList<MatchModel>> GetForAdminAsync(
        Guid competitionId,
        bool includePlayed,
        CancellationToken cancellationToken = default);
}
