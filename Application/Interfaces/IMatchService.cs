using FluentResults;
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
    /// Gets a user's prediction history for a competition, most recent first. Future matches are
    /// only included when <paramref name="includeFuture"/> is true (i.e. the viewer is the user
    /// themselves) - other users only see predictions for matches that have already kicked off.
    /// </summary>
    Task<IReadOnlyList<UserMatchPredictionListItem>> GetUserPredictionHistoryAsync(
        Guid userId,
        Guid competitionId,
        bool includeFuture,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets today's matches for a competition - the Home page's Live updates section and the Live
    /// page - each joined with the given user's own prediction for it (if any), earliest first.
    /// Matches that kicked off shortly before midnight are carried over while they're still
    /// unresolved; see <see cref="Common.LiveDayWindow"/>.
    /// </summary>
    /// <param name="userId">The user whose predictions to include.</param>
    /// <param name="competitionId">The competition to get matches for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<UserMatchPredictionListItem>> GetLiveDayMatchesAsync(
        Guid userId,
        Guid competitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every match for a competition for admin management, optionally excluding already-played
    /// matches, ordered by date.
    /// </summary>
    Task<IReadOnlyList<MatchModel>> GetForAdminAsync(
        Guid competitionId,
        bool includePlayed,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unplayed matches for a competition whose kickoff was at least 90 minutes ago, ordered
    /// by date then home team name - the pool of matches a result can actually be entered for, in
    /// step with the same cut-off <see cref="SaveResultAsync"/> enforces.
    /// </summary>
    Task<IReadOnlyList<MatchModel>> GetForProcessingAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a match's final score and marks it played. Fails with a <see cref="Errors.ConflictError"/>
    /// if the match hasn't been over long enough yet.
    /// </summary>
    Task<Result<MatchModel>> SaveResultAsync(
        Guid matchId,
        int homeTeamGoals,
        int awayTeamGoals,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every played match for a competition, most recent first, each joined with the given
    /// user's own prediction and the average prediction score across all users - for the public
    /// Results page.
    /// </summary>
    Task<IReadOnlyList<MatchListItem>> GetResultsAsync(
        Guid competitionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single played match's result, the given user's own prediction and the average
    /// prediction score across all users, for the Match Detail page. Returns null if the match
    /// doesn't exist in the competition or hasn't been played yet.
    /// </summary>
    Task<MatchListItem?> GetMatchDetailAsync(
        Guid competitionId,
        Guid matchId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
