using FluentResults;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

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

    public async Task<IReadOnlyList<TeamCompetitionModel>> GetAssignedForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var teams = await _dbContext.TeamCompetition
            .Where(tc => tc.CompetitionID == competitionId)
            .OrderBy(tc => tc.Team.TeamName)
            .Select(tc => new TeamCompetitionModel
            {
                TeamCompetitionID = tc.TeamCompetitionID,
                TeamID = tc.TeamID,
                TeamName = tc.Team.TeamName,
            })
            .ToListAsync(cancellationToken);

        return teams;
    }

    public async Task<IReadOnlyList<TeamModel>> GetUnassignedForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var assignedTeamIds = _dbContext.TeamCompetition
            .Where(tc => tc.CompetitionID == competitionId)
            .Select(tc => tc.TeamID);

        var teams = await _dbContext.Team
            .Where(t => !assignedTeamIds.Contains(t.TeamID))
            .OrderBy(t => t.TeamName)
            .ToListAsync(cancellationToken);

        return teams.Adapt<List<TeamModel>>();
    }

    public async Task<Result> AddToCompetitionAsync(Guid competitionId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var teamCompetition = new TeamCompetition
        {
            TeamCompetitionID = Guid.NewGuid(),
            CompetitionID = competitionId,
            TeamID = teamId,
        };

        await _dbContext.AddAsync(teamCompetition, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> RemoveFromCompetitionAsync(Guid teamCompetitionId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TeamCompetition.FirstOrDefaultAsync(tc => tc.TeamCompetitionID == teamCompetitionId, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(new NotFoundError());
        }

        _dbContext.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
