using FluentResults;
using Mapster;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class TeamService : ITeamService
{
    private readonly IApplicationDbContext _dbContext;

    public TeamService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamModel>> GetForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var teams = await _dbContext.TeamCompetition
            .Where(tc => tc.CompetitionID == competitionId)
            .Select(tc => tc.Team)
            .OrderBy(t => t.TeamName)
            .ToListAsync(cancellationToken);

        return teams.Adapt<List<TeamModel>>();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<TeamDetailModel?> GetTeamDetailAsync(Guid competitionId, Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var team = await _dbContext.Team.FirstOrDefaultAsync(t => t.TeamID == teamId, cancellationToken);
        if (team is null)
        {
            return null;
        }

        var playedMatches = await _dbContext.Match
            .Where(m => m.CompetitionID == competitionId && m.MatchPlayed && (m.HomeTeamID == teamId || m.AwayTeamID == teamId))
            .ToListAsync(cancellationToken);

        var homeMatches = playedMatches.Where(m => m.HomeTeamID == teamId && !m.NeutralGround).ToList();
        var awayMatches = playedMatches.Where(m => m.AwayTeamID == teamId && !m.NeutralGround).ToList();
        var neutralMatches = playedMatches.Where(m => m.NeutralGround).ToList();

        var homeGoalsFor = homeMatches.Sum(m => m.HomeTeamGoals ?? 0);
        var homeGoalsAgainst = homeMatches.Sum(m => m.AwayTeamGoals ?? 0);
        var awayGoalsFor = awayMatches.Sum(m => m.AwayTeamGoals ?? 0);
        var awayGoalsAgainst = awayMatches.Sum(m => m.HomeTeamGoals ?? 0);
        var neutralGoalsFor = neutralMatches.Sum(m => m.HomeTeamID == teamId ? (m.HomeTeamGoals ?? 0) : (m.AwayTeamGoals ?? 0));
        var neutralGoalsAgainst = neutralMatches.Sum(m => m.HomeTeamID == teamId ? (m.AwayTeamGoals ?? 0) : (m.HomeTeamGoals ?? 0));

        var goalsFor = homeGoalsFor + awayGoalsFor + neutralGoalsFor;
        var goalsAgainst = homeGoalsAgainst + awayGoalsAgainst + neutralGoalsAgainst;
        var totalMatches = playedMatches.Count;

        var results = await _dbContext.CallStoredProcedureAsync<PredictableMatchListItem>(
            "MatchResultListGet",
            [
                new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
                new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
                new SqlParameter("@TeamID", SqlDbType.UniqueIdentifier) { Value = teamId },
            ],
            cancellationToken);

        return new TeamDetailModel
        {
            TeamID = team.TeamID,
            TeamName = team.TeamName,
            ShortName = team.ShortName,
            ImageName = team.ImageName,
            GoalsFor = goalsFor,
            GoalsAgainst = goalsAgainst,
            AverageGoalsForHome = homeMatches.Count > 0 ? (decimal)homeGoalsFor / homeMatches.Count : null,
            AverageGoalsAgainstHome = homeMatches.Count > 0 ? (decimal)homeGoalsAgainst / homeMatches.Count : null,
            AverageGoalsForAway = awayMatches.Count > 0 ? (decimal)awayGoalsFor / awayMatches.Count : null,
            AverageGoalsAgainstAway = awayMatches.Count > 0 ? (decimal)awayGoalsAgainst / awayMatches.Count : null,
            AverageGoalsForTotal = totalMatches > 0 ? (decimal)goalsFor / totalMatches : null,
            AverageGoalsAgainstTotal = totalMatches > 0 ? (decimal)goalsAgainst / totalMatches : null,
            Results = results,
        };
    }
}
