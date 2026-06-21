using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Interfaces;

public interface ICompetitionService : ICrudService<Guid, CompetitionModel, Competition>
{
    Task<IReadOnlyList<Competition>> GetCompetitionListAsync();

    Task<IReadOnlyList<Competition>> GetCompetitionListForLoginPageAsync();

    Task<IReadOnlyList<UserCompetitionRegistrationListItem>> GetUserCompetitionRegistrationListAsync(Guid userId);

    Task SetUserCompetitionLeagueHistoryAsync();

    Task<IReadOnlyList<CompetitionRealLeagueTableItem>> CompetitionRealLeagueTableGetAsync(Guid competitionId);

    Task<IReadOnlyList<CompetitionUserLeagueTableItem>> CompetitionUserLeagueTableGetAsync(Guid competitionId, Guid userId);

    Task<IList<System.DateTime>> GetCompetitionWeeksAsync(Guid competitionId);
}
