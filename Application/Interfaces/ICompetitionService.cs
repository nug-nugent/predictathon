using FluentResults;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Interfaces;

// TCreateModel: CreateCompetitionModel (client-supplied fields)
// TEditModel: CompetitionModel (full model including CompetitionID)
public interface ICompetitionService : ICrudService<Guid, CreateCompetitionModel, CompetitionModel, Competition>
{
    Task<IReadOnlyList<Competition>> GetCompetitionListAsync();

    Task<IReadOnlyList<Competition>> GetCompetitionListForLoginPageAsync();

    Task<IReadOnlyList<UserCompetitionRegistrationListItem>> GetUserCompetitionRegistrationListAsync(Guid userId);

    /// <summary>
    /// Marks a competition as the user's default (unmarking any other), for it to be preselected on
    /// future logins. Fails with a <see cref="Errors.NotFoundError"/> if the user isn't registered for it.
    /// </summary>
    Task<Result> SetDefaultCompetitionAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken = default);

    Task SetUserCompetitionLeagueHistoryAsync();

    Task<IReadOnlyList<CompetitionRealLeagueTableItem>> CompetitionRealLeagueTableGetAsync(Guid competitionId);

    Task<IReadOnlyList<CompetitionUserLeagueTableItem>> CompetitionUserLeagueTableGetAsync(Guid competitionId, Guid userId);

    Task<IList<System.DateTime>> GetCompetitionWeeksAsync(Guid competitionId);
}
