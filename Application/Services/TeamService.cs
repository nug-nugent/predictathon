using Mapster;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;

namespace Predictathon.Application.Services;

[ScopedService]
public class TeamService : ITeamService
{
    private readonly IApplicationDbContext _dbContext;

    public TeamService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TeamModel>> GetForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var teams = await _dbContext.TeamCompetition
            .Where(tc => tc.CompetitionID == competitionId)
            .Select(tc => tc.Team)
            .OrderBy(t => t.TeamName)
            .ToListAsync(cancellationToken);

        return teams.Adapt<List<TeamModel>>();
    }
}
