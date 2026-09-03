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
    /// <summary>The most recent results any one caller can ask for in a single request.</summary>
    private const int MaximumRecentResults = 20;

    private readonly IApplicationDbContext _dbContext;

    public TeamService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamModel>> GetForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var teams = await _dbContext.TeamCompetition
            .AsNoTracking()
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
            .AsNoTracking()
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
        var team = await _dbContext.Team.AsNoTracking().FirstOrDefaultAsync(t => t.TeamID == teamId, cancellationToken);
        if (team is null)
        {
            return null;
        }

        var competitionMatches = await _dbContext.Match
            .AsNoTracking()
            .Where(m => m.CompetitionID == competitionId)
            .ToListAsync(cancellationToken);

        var playedMatches = competitionMatches
            .Where(m => m.MatchPlayed && (m.HomeTeamID == teamId || m.AwayTeamID == teamId))
            .ToList();

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

        var teamsById = await GetCompetitionTeamsAsync(competitionId, competitionMatches, cancellationToken);
        var fixtures = BuildFixtures(competitionMatches, teamId, teamsById);

        // A competition containing any knockout match (a World Cup's group stage plus last 16, say)
        // has no single meaningful table, so the page hides it rather than showing a misleading one.
        var leagueTable = competitionMatches.Any(m => m.Knockout)
            ? null
            : BuildLeagueTable(competitionMatches, teamsById);

        var results = await _dbContext.CallStoredProcedureAsync<MatchListItem>(
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
            Fixtures = fixtures,
            LeagueTable = leagueTable,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamRecentResultItem>> GetRecentResultsAsync(Guid competitionId, Guid teamId, int count, CancellationToken cancellationToken = default)
    {
        // The popup this feeds shows a handful of matches, so keep the query bounded whatever the
        // caller asks for.
        var take = Math.Clamp(count, 1, MaximumRecentResults);

        var matches = await _dbContext.Match
            .AsNoTracking()
            .Where(m => m.CompetitionID == competitionId && m.MatchPlayed && (m.HomeTeamID == teamId || m.AwayTeamID == teamId))
            .OrderByDescending(m => m.MatchDateTime)
            .Take(take)
            .ToListAsync(cancellationToken);

        var teamIds = matches
            .SelectMany(m => new[] { m.HomeTeamID, m.AwayTeamID })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var teamsById = await _dbContext.Team
            .Where(t => teamIds.Contains(t.TeamID))
            .ToDictionaryAsync(t => t.TeamID, cancellationToken);

        return matches
            .Select(m =>
            {
                var homeTeam = m.HomeTeamID.HasValue && teamsById.TryGetValue(m.HomeTeamID.Value, out var home) ? home : null;
                var awayTeam = m.AwayTeamID.HasValue && teamsById.TryGetValue(m.AwayTeamID.Value, out var away) ? away : null;
                var homeGoals = m.HomeTeamGoals ?? 0;
                var awayGoals = m.AwayTeamGoals ?? 0;
                var wasHome = m.HomeTeamID == teamId;
                var goalsFor = wasHome ? homeGoals : awayGoals;
                var goalsAgainst = wasHome ? awayGoals : homeGoals;

                return new TeamRecentResultItem
                {
                    MatchID = m.MatchID,
                    MatchDateTime = m.MatchDateTime,
                    HomeTeamID = m.HomeTeamID,
                    // Mirrors BuildFixtures' handling of a not-yet-decided knockout slot.
                    HomeTeam = homeTeam?.TeamName ?? m.HomeTeamTBC,
                    HomeTeamShortName = homeTeam?.ShortName ?? "TBC",
                    HomeTeamImage = homeTeam?.ImageName,
                    AwayTeamID = m.AwayTeamID,
                    AwayTeam = awayTeam?.TeamName ?? m.AwayTeamTBC,
                    AwayTeamShortName = awayTeam?.ShortName ?? "TBC",
                    AwayTeamImage = awayTeam?.ImageName,
                    HomeTeamGoals = homeGoals,
                    AwayTeamGoals = awayGoals,
                    NeutralGround = m.NeutralGround,
                    Description = m.Description,
                    Knockout = m.Knockout,
                    Outcome = goalsFor > goalsAgainst ? "Win" : goalsFor == goalsAgainst ? "Draw" : "Loss",
                };
            })
            .ToList();
    }

    /// <summary>
    /// Loads every team involved in a competition - those assigned to it plus any appearing in one of
    /// its matches - keyed by team id, so fixtures and the league table can be projected in memory.
    /// </summary>
    /// <param name="competitionId">The competition to load teams for.</param>
    /// <param name="competitionMatches">The competition's matches, already loaded.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<Dictionary<Guid, Team>> GetCompetitionTeamsAsync(Guid competitionId, IReadOnlyList<Match> competitionMatches, CancellationToken cancellationToken)
    {
        var assignedTeamIds = await _dbContext.TeamCompetition
            .Where(tc => tc.CompetitionID == competitionId)
            .Select(tc => tc.TeamID)
            .ToListAsync(cancellationToken);

        var teamIds = assignedTeamIds
            .Concat(competitionMatches.SelectMany(m => new[] { m.HomeTeamID, m.AwayTeamID }).Where(id => id.HasValue).Select(id => id!.Value))
            .Distinct()
            .ToList();

        return await _dbContext.Team
            .Where(t => teamIds.Contains(t.TeamID))
            .ToDictionaryAsync(t => t.TeamID, cancellationToken);
    }

    /// <summary>
    /// Projects a team's not-yet-played matches in a competition into fixture rows, soonest first.
    /// </summary>
    /// <param name="competitionMatches">The competition's matches, already loaded.</param>
    /// <param name="teamId">The team whose fixtures are wanted.</param>
    /// <param name="teamsById">The competition's teams, keyed by team id.</param>
    private static List<TeamFixtureItem> BuildFixtures(IReadOnlyList<Match> competitionMatches, Guid teamId, Dictionary<Guid, Team> teamsById)
    {
        return competitionMatches
            .Where(m => !m.MatchPlayed && (m.HomeTeamID == teamId || m.AwayTeamID == teamId))
            .OrderBy(m => m.MatchDateTime)
            .Select(m =>
            {
                var homeTeam = m.HomeTeamID.HasValue && teamsById.TryGetValue(m.HomeTeamID.Value, out var home) ? home : null;
                var awayTeam = m.AwayTeamID.HasValue && teamsById.TryGetValue(m.AwayTeamID.Value, out var away) ? away : null;

                return new TeamFixtureItem
                {
                    MatchID = m.MatchID,
                    MatchDateTime = m.MatchDateTime,
                    HomeTeamID = m.HomeTeamID,
                    // Mirrors MatchListGet's ISNULL(TeamName, HomeTeamTBC) / 'TBC' handling of a
                    // not-yet-decided knockout slot.
                    HomeTeam = homeTeam?.TeamName ?? m.HomeTeamTBC,
                    HomeTeamShortName = homeTeam?.ShortName ?? "TBC",
                    HomeTeamImage = homeTeam?.ImageName,
                    AwayTeamID = m.AwayTeamID,
                    AwayTeam = awayTeam?.TeamName ?? m.AwayTeamTBC,
                    AwayTeamShortName = awayTeam?.ShortName ?? "TBC",
                    AwayTeamImage = awayTeam?.ImageName,
                    NeutralGround = m.NeutralGround,
                    Description = m.Description,
                    Knockout = m.Knockout,
                };
            })
            .ToList();
    }

    /// <summary>
    /// Builds a competition's actual league table from its played matches - three points for a win,
    /// one for a draw - ordered by points, then goal difference, then goals scored, then team name.
    /// </summary>
    /// <param name="competitionMatches">The competition's matches, already loaded.</param>
    /// <param name="teamsById">The competition's teams, keyed by team id.</param>
    private static List<TeamStandingItem> BuildLeagueTable(IReadOnlyList<Match> competitionMatches, Dictionary<Guid, Team> teamsById)
    {
        var standings = teamsById.Values.ToDictionary(
            t => t.TeamID,
            t => new TeamStandingItem
            {
                TeamID = t.TeamID,
                TeamName = t.TeamName,
                ShortName = t.ShortName,
                ImageName = t.ImageName,
            });

        var playedMatches = competitionMatches.Where(m => m.MatchPlayed && m.HomeTeamID.HasValue && m.AwayTeamID.HasValue);

        foreach (var match in playedMatches)
        {
            if (!standings.TryGetValue(match.HomeTeamID!.Value, out var home) || !standings.TryGetValue(match.AwayTeamID!.Value, out var away))
            {
                continue;
            }

            var homeGoals = match.HomeTeamGoals ?? 0;
            var awayGoals = match.AwayTeamGoals ?? 0;

            ApplyResult(home, homeGoals, awayGoals);
            ApplyResult(away, awayGoals, homeGoals);
        }

        var ordered = standings.Values
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.GoalDifference)
            .ThenByDescending(s => s.GoalsFor)
            .ThenBy(s => s.TeamName)
            .ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].Position = index + 1;
        }

        return ordered;
    }

    /// <summary>
    /// Adds one played match to a team's league-table row, from that team's point of view.
    /// </summary>
    /// <param name="standing">The team's league-table row.</param>
    /// <param name="goalsFor">Goals the team scored in the match.</param>
    /// <param name="goalsAgainst">Goals the team conceded in the match.</param>
    private static void ApplyResult(TeamStandingItem standing, int goalsFor, int goalsAgainst)
    {
        standing.Played++;
        standing.GoalsFor += goalsFor;
        standing.GoalsAgainst += goalsAgainst;
        standing.GoalDifference = standing.GoalsFor - standing.GoalsAgainst;

        if (goalsFor > goalsAgainst)
        {
            standing.Won++;
            standing.Points += 3;
        }
        else if (goalsFor == goalsAgainst)
        {
            standing.Drawn++;
            standing.Points += 1;
        }
        else
        {
            standing.Lost++;
        }
    }
}
