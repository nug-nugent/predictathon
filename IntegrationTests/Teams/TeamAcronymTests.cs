using FluentAssertions;
using MapsterMapper;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;
using Predictathon.IntegrationTests.TestDoubles;

namespace Predictathon.IntegrationTests.Teams;

/// <summary>
/// Covers dbo.Team.Acronym reaching every stored procedure that names a team. Four of those
/// procedures aggregate, so the acronym has to appear in their GROUP BY as well as their SELECT -
/// a column dropped from one of them fails silently as a null, which is indistinguishable from a
/// team that genuinely has no acronym, so it is checked here against real SQL rather than inferred.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class TeamAcronymTests
{
    private readonly DatabaseFixture _fixture;

    public TeamAcronymTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static MatchService MakeMatchService(ApplicationDbContext dbContext)
    {
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateMatchModel, MatchModel>(dbContext, new Mapper());
        return new MatchService(dependencyAggregate, dbContext, dbContext, new LeagueDataCache());
    }

    private static CompetitionService MakeCompetitionService(ApplicationDbContext dbContext)
    {
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateCompetitionModel, CompetitionModel>(dbContext, new Mapper());
        return new CompetitionService(dependencyAggregate, dbContext);
    }

    private static StatisticsService MakeStatisticsService(ApplicationDbContext dbContext)
    {
        return new StatisticsService(dbContext, new StubAvatarService(), new LeagueDataCache());
    }

    [Fact]
    public async Task EveryStoredProcedureThatNamesATeam_ReturnsItsAcronymAlongsideItsShortName()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        };
        dbContext.Competition.Add(competition);

        var you = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"you-{Guid.NewGuid():N}" };
        var otherUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"other-{Guid.NewGuid():N}" };
        dbContext.Users.AddRange(you, otherUser);

        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "Homeside", Acronym = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "Awayside", Acronym = "AWY" };
        dbContext.Team.AddRange(home, away);

        // AverageScoreByTeamListGet only considers teams registered for the competition.
        dbContext.TeamCompetition.AddRange(
            new TeamCompetition { TeamCompetitionID = Guid.NewGuid(), CompetitionID = competition.CompetitionID, TeamID = home.TeamID },
            new TeamCompetition { TeamCompetitionID = Guid.NewGuid(), CompetitionID = competition.CompetitionID, TeamID = away.TeamID });

        var playedMatch = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddDays(-1),
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = true,
            HomeTeamGoals = 2,
            AwayTeamGoals = 1,
        };
        var upcomingMatch = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddDays(1),
            HomeTeamID = away.TeamID,
            AwayTeamID = home.TeamID,
            MatchPlayed = false,
        };
        dbContext.Match.AddRange(playedMatch, upcomingMatch);

        // A spot-on prediction against a poor one, so the played match has an average to beat and
        // MatchPredictionAverageBiggestDifferencesGet (which only returns above-average rows) has
        // something to return.
        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = playedMatch.MatchID, UserID = you.Id, HomeTeamGoals = 2, AwayTeamGoals = 1, Score = 3, GoalDifference = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = playedMatch.MatchID, UserID = otherUser.Id, HomeTeamGoals = 0, AwayTeamGoals = 3, Score = 0, GoalDifference = -3 });

        await dbContext.SaveChangesAsync();

        try
        {
            var matchService = MakeMatchService(dbContext);
            var competitionService = MakeCompetitionService(dbContext);
            var statisticsService = MakeStatisticsService(dbContext);

            // UserMatchPredictionListGet.
            var predictions = await matchService.GetUserPredictionHistoryAsync(you.Id, competition.CompetitionID, includeFuture: true);
            var predictedMatch = predictions.Single(m => m.MatchID == playedMatch.MatchID);
            predictedMatch.HomeTeamShortName.Should().Be("Homeside");
            predictedMatch.HomeTeamAcronym.Should().Be("HOM");
            predictedMatch.AwayTeamAcronym.Should().Be("AWY");

            // MatchResultListGet.
            var results = await matchService.GetResultsAsync(competition.CompetitionID, you.Id);
            var result = results.Single(m => m.MatchID == playedMatch.MatchID);
            result.HomeTeamAcronym.Should().Be("HOM");
            result.AwayTeamAcronym.Should().Be("AWY");

            // CompetitionRealLeagueTableGet.
            var realTable = await competitionService.CompetitionRealLeagueTableGetAsync(competition.CompetitionID);
            realTable.Single(r => r.TeamID == home.TeamID).Acronym.Should().Be("HOM");
            realTable.Single(r => r.TeamID == away.TeamID).Acronym.Should().Be("AWY");

            // CompetitionUserLeagueTableGet.
            var userTable = await competitionService.CompetitionUserLeagueTableGetAsync(competition.CompetitionID, you.Id);
            userTable.Single(r => r.TeamID == home.TeamID).Acronym.Should().Be("HOM");
            userTable.Single(r => r.TeamID == away.TeamID).Acronym.Should().Be("AWY");

            // AverageScoreByTeamListGet, plus MatchResultListGet again via the same call.
            var statistics = await statisticsService.GetCurrentCompetitionStatisticsAsync(competition.CompetitionID, you.Id);
            statistics.PredictableTeams.Single(t => t.TeamID == home.TeamID).Acronym.Should().Be("HOM");
            statistics.PredictableTeams.Single(t => t.TeamID == away.TeamID).Acronym.Should().Be("AWY");

            // MatchPredictionAverageBiggestDifferencesGet.
            var bestPredictions = await statisticsService.GetBestPredictionsAsync(competition.CompetitionID);
            var best = bestPredictions.Single(p => p.MatchID == playedMatch.MatchID && p.UserID == you.Id);
            best.HomeTeamAcronym.Should().Be("HOM");
            best.AwayTeamAcronym.Should().Be("AWY");
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID,
                [playedMatch.MatchID, upcomingMatch.MatchID],
                [you.Id, otherUser.Id],
                [home.TeamID, away.TeamID]);
        }
    }

    /// <summary>
    /// A team with no acronym yet comes back null rather than blank or defaulted, which is what the
    /// UI's fall back to the short name keys off.
    /// </summary>
    [Fact]
    public async Task ATeamWithNoAcronym_ComesBackAsNull()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        };
        dbContext.Competition.Add(competition);

        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"user-{Guid.NewGuid():N}" };
        dbContext.Users.Add(user);

        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "Homeside" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "Awayside" };
        dbContext.Team.AddRange(home, away);

        var match = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddDays(-1),
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = true,
            HomeTeamGoals = 1,
            AwayTeamGoals = 0,
        };
        dbContext.Match.Add(match);

        await dbContext.SaveChangesAsync();

        try
        {
            var results = await MakeMatchService(dbContext).GetResultsAsync(competition.CompetitionID, user.Id);

            var result = results.Single(m => m.MatchID == match.MatchID);
            result.HomeTeamShortName.Should().Be("Homeside");
            result.HomeTeamAcronym.Should().BeNull();
            result.AwayTeamAcronym.Should().BeNull();
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [user.Id], [home.TeamID, away.TeamID]);
        }
    }

    /// <summary>
    /// Removes the rows a test created, in foreign-key order.
    /// </summary>
    /// <param name="dbContext">The context the rows were created through.</param>
    /// <param name="competitionId">The competition to remove, along with its team assignments.</param>
    /// <param name="matchIds">The matches to remove, along with their predictions.</param>
    /// <param name="userIds">The users to remove.</param>
    /// <param name="teamIds">The teams to remove.</param>
    private static async Task CleanUpAsync(
        ApplicationDbContext dbContext,
        Guid competitionId,
        IReadOnlyList<Guid> matchIds,
        IReadOnlyList<Guid> userIds,
        IReadOnlyList<Guid> teamIds)
    {
        dbContext.Prediction.RemoveRange(dbContext.Prediction.Where(p => matchIds.Contains(p.MatchID)));
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => matchIds.Contains(m.MatchID)));
        dbContext.TeamCompetition.RemoveRange(dbContext.TeamCompetition.Where(tc => tc.CompetitionID == competitionId));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        dbContext.Team.RemoveRange(dbContext.Team.Where(t => teamIds.Contains(t.TeamID)));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => c.CompetitionID == competitionId));
        await dbContext.SaveChangesAsync();
    }
}
