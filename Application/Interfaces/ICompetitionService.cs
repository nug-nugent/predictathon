using FluentResults;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Interfaces;

// TCreateModel: CreateCompetitionModel (client-supplied fields)
// TEditModel: CompetitionModel (full model including CompetitionID)
public interface ICompetitionService : ICrudService<Guid, CreateCompetitionModel, CompetitionModel, Competition>
{
    /// <summary>
    /// Gets every competition, most recently started first.
    /// </summary>
    Task<IReadOnlyList<Competition>> GetCompetitionListAsync();

    /// <summary>
    /// Gets the competitions available for self-registration from the login page, most recently
    /// started first.
    /// </summary>
    Task<IReadOnlyList<Competition>> GetCompetitionListForLoginPageAsync();

    /// <summary>
    /// Gets the list of competitions a user is registered for, for display on their registrations page.
    /// </summary>
    /// <param name="userId">The user to get registrations for.</param>
    Task<IReadOnlyList<UserCompetitionRegistrationListItem>> GetUserCompetitionRegistrationListAsync(Guid userId);

    /// <summary>
    /// Gets a user's league-position history for a competition, ordered by date ascending.
    /// </summary>
    Task<IReadOnlyList<UserCompetitionLeagueHistoryItem>> GetUserCompetitionLeagueHistoryAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a competition as the user's default (unmarking any other), for it to be preselected on
    /// future logins. Fails with a <see cref="Errors.NotFoundError"/> if the user isn't registered for it.
    /// </summary>
    Task<Result> SetDefaultCompetitionAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Snapshots each competition's current league table into that day's history record, so past
    /// standings remain viewable after later weeks change the table.
    /// </summary>
    Task SetUserCompetitionLeagueHistoryAsync();

    /// <summary>
    /// Gets the real-world league table for a competition.
    /// </summary>
    /// <param name="competitionId">The competition to get the table for.</param>
    Task<IReadOnlyList<CompetitionRealLeagueTableItem>> CompetitionRealLeagueTableGetAsync(Guid competitionId);

    /// <summary>
    /// Gets a competition's prediction league table alongside the given user's own ranking context.
    /// </summary>
    /// <param name="competitionId">The competition to get the table for.</param>
    /// <param name="userId">The user whose position to highlight.</param>
    Task<IReadOnlyList<CompetitionUserLeagueTableItem>> CompetitionUserLeagueTableGetAsync(Guid competitionId, Guid userId);

    /// <summary>
    /// Gets the distinct Friday-to-Thursday match weeks for a competition, ordered ascending.
    /// </summary>
    /// <param name="competitionId">The competition to get weeks for.</param>
    Task<IList<System.DateTime>> GetCompetitionWeeksAsync(Guid competitionId);

    /// <summary>
    /// Gets the distinct Friday-to-Thursday match weeks for a competition, ordered ascending, each
    /// summarised for the given user: when the week's last match kicks off, and how many matches in
    /// it they have yet to predict but still can.
    /// </summary>
    /// <param name="competitionId">The competition to get weeks for.</param>
    /// <param name="userId">The user whose outstanding predictions to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<CompetitionWeekSummary>> GetCompetitionWeekSummariesAsync(Guid competitionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every competition series, in display order - reference data for the admin form's
    /// series picker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<CompetitionSeriesModel>> GetCompetitionSeriesListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets site-wide stats shown on the pre-login landing page.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PublicStatsModel> GetPublicStatsAsync(CancellationToken cancellationToken = default);
}
