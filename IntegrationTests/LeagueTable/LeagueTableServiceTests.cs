using FluentAssertions;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;

namespace Predictathon.IntegrationTests.LeagueTable;

/// <summary>
/// Exercises dbo.LeagueTableGet, the aggregate query that turns per-match Prediction.Score rows into
/// league positions - a set-based SQL aggregate that isn't a good fit for translation into (and
/// re-testing as) LINQ, so it's covered here against a real SQL Server instance instead.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class LeagueTableServiceTests
{
    private readonly DatabaseFixture _fixture;

    public LeagueTableServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetLeagueTableAsync_RanksUsersByTotalScore_ThenGoalDifference()
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

        var leader = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"leader-{Guid.NewGuid():N}" };
        var runnerUp = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"runner-up-{Guid.NewGuid():N}" };
        var noPredictionsUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"no-predictions-{Guid.NewGuid():N}" };
        dbContext.Users.AddRange(leader, runnerUp, noPredictionsUser);

        dbContext.UserCompetition.AddRange(
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = leader.Id, CompetitionID = competition.CompetitionID },
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = runnerUp.Id, CompetitionID = competition.CompetitionID },
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = noPredictionsUser.Id, CompetitionID = competition.CompetitionID });

        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "AWY" };
        dbContext.Team.AddRange(home, away);

        var matchOne = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddDays(-2),
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = true,
            HomeTeamGoals = 2,
            AwayTeamGoals = 0,
        };
        var matchTwo = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddDays(-1),
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = true,
            HomeTeamGoals = 1,
            AwayTeamGoals = 1,
        };
        dbContext.Match.AddRange(matchOne, matchTwo);

        // Leader: two 3-pointers (6 points total). Runner-up: two 1-pointers (2 points total).
        // noPredictionsUser predicts neither match, so should still appear (via the LEFT JOIN) with 0.
        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = matchOne.MatchID, UserID = leader.Id, HomeTeamGoals = 2, AwayTeamGoals = 0, Score = 3, GoalDifference = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = matchTwo.MatchID, UserID = leader.Id, HomeTeamGoals = 1, AwayTeamGoals = 1, Score = 3, GoalDifference = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = matchOne.MatchID, UserID = runnerUp.Id, HomeTeamGoals = 1, AwayTeamGoals = 0, Score = 1, GoalDifference = -1 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = matchTwo.MatchID, UserID = runnerUp.Id, HomeTeamGoals = 2, AwayTeamGoals = 2, Score = 1, GoalDifference = -2 });

        await dbContext.SaveChangesAsync();

        try
        {
            var service = new LeagueTableService(dbContext);

            var table = await service.GetLeagueTableAsync(competition.CompetitionID);

            var leaderRow = table.Single(r => r.UserID == leader.Id);
            var runnerUpRow = table.Single(r => r.UserID == runnerUp.Id);
            var noPredictionsRow = table.Single(r => r.UserID == noPredictionsUser.Id);

            leaderRow.Score.Should().Be(6);
            leaderRow.ThreePointers.Should().Be(2);
            leaderRow.LeaguePosition.Should().Be(1);

            runnerUpRow.Score.Should().Be(2);
            runnerUpRow.OnePointers.Should().Be(2);
            runnerUpRow.LeaguePosition.Should().Be(2);

            noPredictionsRow.Score.Should().Be(0);
            noPredictionsRow.NoPredictions.Should().Be(2);
            noPredictionsRow.LeaguePosition.Should().Be(3);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID,
                [matchOne.MatchID, matchTwo.MatchID],
                [leader.Id, runnerUp.Id, noPredictionsUser.Id],
                [home.TeamID, away.TeamID]);
        }
    }

    private static async Task CleanUpAsync(
        ApplicationDbContext dbContext,
        Guid competitionId,
        IReadOnlyList<Guid> matchIds,
        IReadOnlyList<Guid> userIds,
        IReadOnlyList<Guid> teamIds)
    {
        dbContext.Prediction.RemoveRange(dbContext.Prediction.Where(p => matchIds.Contains(p.MatchID)));
        dbContext.UserCompetition.RemoveRange(dbContext.UserCompetition.Where(uc => uc.CompetitionID == competitionId));
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => matchIds.Contains(m.MatchID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        dbContext.Team.RemoveRange(dbContext.Team.Where(t => teamIds.Contains(t.TeamID)));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => c.CompetitionID == competitionId));
        await dbContext.SaveChangesAsync();
    }
}
