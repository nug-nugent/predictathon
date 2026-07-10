using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class CompetitionService : CrudService<Guid, CreateCompetitionModel, CompetitionModel, Competition>, ICompetitionService
{
    private readonly IApplicationDbContext _appDbContext;

    public CompetitionService(
        ICrudServiceDependencyAggregate<CreateCompetitionModel, CompetitionModel> dependencyAggregate,
        IApplicationDbContext appDbContext
    ) : base(dependencyAggregate)
    {
        _appDbContext = appDbContext ?? throw new ArgumentNullException(nameof(appDbContext));
    }

    public async Task<IReadOnlyList<Competition>> GetCompetitionListAsync()
    {
        return await _appDbContext.Competition.OrderByDescending(c => c.StartDate).ToListAsync();
    }

    public async Task<IReadOnlyList<Competition>> GetCompetitionListForLoginPageAsync()
    {
        return await _appDbContext.Competition.Where(c => c.RegistrationAvailableOnLoginPage).OrderByDescending(c => c.StartDate).ToListAsync();
    }

    public async Task<IReadOnlyList<UserCompetitionRegistrationListItem>> GetUserCompetitionRegistrationListAsync(Guid userId)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId }
        };

        var results = await _appDbContext.CallStoredProcedureAsync<UserCompetitionRegistrationListItem>("UserCompetitionRegistrationListGet", parameters);

        return results;
    }

    public async Task SetUserCompetitionLeagueHistoryAsync()
    {
        var competitions = await _appDbContext.Competition.ToListAsync();

        foreach (var comp in competitions)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Date", SqlDbType.Date) { Value = DateTime.Today },
                new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = comp.CompetitionID }
            };

            await _appDbContext.CallStoredProcedureAsync("UserCompetitionLeagueHistorySet", parameters);
        }
    }

    public async Task<IReadOnlyList<CompetitionRealLeagueTableItem>> CompetitionRealLeagueTableGetAsync(Guid competitionId)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId }
        };

        var results = await _appDbContext.CallStoredProcedureAsync<CompetitionRealLeagueTableItem>("CompetitionRealLeagueTableGet", parameters);

        return results;
    }

    public async Task<IReadOnlyList<CompetitionUserLeagueTableItem>> CompetitionUserLeagueTableGetAsync(Guid competitionId, Guid userId)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId }
        };

        var results = await _appDbContext.CallStoredProcedureAsync<CompetitionUserLeagueTableItem>("CompetitionUserLeagueTableGet", parameters);

        return results;
    }

    public async Task<IList<DateTime>> GetCompetitionWeeksAsync(Guid competitionId)
    {
        var knownFriday = new DateTime(1990, 1, 5);

        var dates = await _appDbContext.Match
            .Where(m => m.CompetitionID == competitionId)
            .Select(m => m.MatchDateTime.Date)
            .Distinct()
            .ToListAsync();

        var weeks = dates.Select(d =>
        {
            var diff = (int)(d - knownFriday).TotalDays;
            var mod = ((diff % 7) + 7) % 7;
            return d.AddDays(-mod);
        })
        .Distinct()
        .OrderBy(d => d)
        .ToList();

        return weeks;
    }
}
