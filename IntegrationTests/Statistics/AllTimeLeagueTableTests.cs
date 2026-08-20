using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;

namespace Predictathon.IntegrationTests.Statistics;

/// <summary>
/// Exercises dbo.Statistics_AllTimeLeagueTableGet, the aggregate query that ranks users by their
/// career totals across every competition they've ever been registered for - a set-based SQL
/// aggregate that isn't a good fit for translation into (and re-testing as) LINQ, so it's covered
/// here against a real SQL Server instance instead.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AllTimeLeagueTableTests
{
    private readonly DatabaseFixture _fixture;

    public AllTimeLeagueTableTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAllTimeLeagueTableAsync_AggregatesScoresAcrossMultipleCompetitions()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competitionOne = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        };
        var competitionTwo = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        };
        dbContext.Competition.AddRange(competitionOne, competitionTwo);

        // Leader plays in both competitions; runner-up only plays in the second.
        var leader = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"leader-{Guid.NewGuid():N}" };
        var runnerUp = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"runner-up-{Guid.NewGuid():N}" };
        dbContext.Users.AddRange(leader, runnerUp);

        dbContext.UserCompetition.AddRange(
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = leader.Id, CompetitionID = competitionOne.CompetitionID },
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = leader.Id, CompetitionID = competitionTwo.CompetitionID },
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = runnerUp.Id, CompetitionID = competitionTwo.CompetitionID });

        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "AWY" };
        dbContext.Team.AddRange(home, away);

        var matchInCompetitionOne = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competitionOne.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddDays(-45),
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = true,
            HomeTeamGoals = 2,
            AwayTeamGoals = 0,
        };
        var matchInCompetitionTwo = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competitionTwo.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddDays(-1),
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = true,
            HomeTeamGoals = 1,
            AwayTeamGoals = 1,
        };
        dbContext.Match.AddRange(matchInCompetitionOne, matchInCompetitionTwo);

        // Leader: a 3-pointer in each competition (6 points total, spanning both). Runner-up: a
        // single 1-pointer in the second competition only, plus a played match with no prediction.
        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = matchInCompetitionOne.MatchID, UserID = leader.Id, HomeTeamGoals = 2, AwayTeamGoals = 0, Score = 3, GoalDifference = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = matchInCompetitionTwo.MatchID, UserID = leader.Id, HomeTeamGoals = 1, AwayTeamGoals = 1, Score = 3, GoalDifference = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = matchInCompetitionTwo.MatchID, UserID = runnerUp.Id, HomeTeamGoals = 2, AwayTeamGoals = 1, Score = 1, GoalDifference = -1 });

        await dbContext.SaveChangesAsync();

        try
        {
            var service = new StatisticsService(dbContext);

            var table = await service.GetAllTimeLeagueTableAsync();

            var leaderRow = table.Single(r => r.UserID == leader.Id);
            var runnerUpRow = table.Single(r => r.UserID == runnerUp.Id);

            leaderRow.Score.Should().Be(6);
            leaderRow.ThreePointers.Should().Be(2);
            leaderRow.LeaguePosition.Should().Be(1);

            runnerUpRow.Score.Should().Be(1);
            runnerUpRow.OnePointers.Should().Be(1);
            runnerUpRow.NoPredictions.Should().Be(1);
            runnerUpRow.LeaguePosition.Should().Be(2);
        }
        finally
        {
            await CleanUpAsync(dbContext,
                [competitionOne.CompetitionID, competitionTwo.CompetitionID],
                [matchInCompetitionOne.MatchID, matchInCompetitionTwo.MatchID],
                [leader.Id, runnerUp.Id],
                [home.TeamID, away.TeamID]);
        }
    }

    private static async Task CleanUpAsync(
        ApplicationDbContext dbContext,
        IReadOnlyList<Guid> competitionIds,
        IReadOnlyList<Guid> matchIds,
        IReadOnlyList<Guid> userIds,
        IReadOnlyList<Guid> teamIds)
    {
        dbContext.Prediction.RemoveRange(dbContext.Prediction.Where(p => matchIds.Contains(p.MatchID)));
        dbContext.UserCompetition.RemoveRange(dbContext.UserCompetition.Where(uc => competitionIds.Contains(uc.CompetitionID)));
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => matchIds.Contains(m.MatchID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        dbContext.Team.RemoveRange(dbContext.Team.Where(t => teamIds.Contains(t.TeamID)));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => competitionIds.Contains(c.CompetitionID)));
        await dbContext.SaveChangesAsync();
    }
}
