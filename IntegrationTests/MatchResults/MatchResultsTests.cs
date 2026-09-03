using FluentAssertions;
using MapsterMapper;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;

namespace Predictathon.IntegrationTests.MatchResults;

/// <summary>
/// Exercises dbo.MatchResultListGet, in particular the @MatchID filter added for the Match Detail
/// page - a set-based SQL aggregate (average prediction score per match) that isn't a good fit for
/// translation into (and re-testing as) LINQ, so it's covered here against a real SQL Server
/// instance instead.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class MatchResultsTests
{
    private readonly DatabaseFixture _fixture;

    public MatchResultsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static MatchService MakeService(ApplicationDbContext dbContext)
    {
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateMatchModel, MatchModel>(dbContext, new Mapper());
        return new MatchService(dependencyAggregate, dbContext, dbContext, new LeagueDataCache());
    }

    [Fact]
    public async Task GetResultsAsync_ReturnsOnlyPlayedMatchesForTheCompetition_MostRecentFirst()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        };
        var otherCompetition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        };
        dbContext.Competition.AddRange(competition, otherCompetition);

        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"user-{Guid.NewGuid():N}" };
        dbContext.Users.Add(user);

        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "AWY" };
        dbContext.Team.AddRange(home, away);

        var earlierMatch = new Match
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
        var laterMatch = new Match
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
        var unplayedMatch = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddDays(1),
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = false,
        };
        var otherCompetitionMatch = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = otherCompetition.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddDays(-3),
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = true,
            HomeTeamGoals = 3,
            AwayTeamGoals = 3,
        };
        dbContext.Match.AddRange(earlierMatch, laterMatch, unplayedMatch, otherCompetitionMatch);

        await dbContext.SaveChangesAsync();

        try
        {
            var service = MakeService(dbContext);

            var results = await service.GetResultsAsync(competition.CompetitionID, user.Id);

            results.Select(r => r.MatchID).Should().Equal(laterMatch.MatchID, earlierMatch.MatchID);
        }
        finally
        {
            await CleanUpAsync(dbContext,
                [competition.CompetitionID, otherCompetition.CompetitionID],
                [earlierMatch.MatchID, laterMatch.MatchID, unplayedMatch.MatchID, otherCompetitionMatch.MatchID],
                [user.Id],
                [home.TeamID, away.TeamID]);
        }
    }

    [Fact]
    public async Task GetMatchDetailAsync_ReturnsTheRequestedMatchWithYourAndAveragePredictionScores()
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

        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "AWY" };
        dbContext.Team.AddRange(home, away);

        var match = new Match
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
        var unplayedMatch = new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = DateTime.UtcNow.AddDays(1),
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = false,
        };
        dbContext.Match.AddRange(match, unplayedMatch);

        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = you.Id, HomeTeamGoals = 2, AwayTeamGoals = 1, Score = 3, GoalDifference = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = otherUser.Id, HomeTeamGoals = 1, AwayTeamGoals = 1, Score = 1, GoalDifference = -1 });

        await dbContext.SaveChangesAsync();

        try
        {
            var service = MakeService(dbContext);

            var detail = await service.GetMatchDetailAsync(competition.CompetitionID, match.MatchID, you.Id);
            var notFoundForUnplayedMatch = await service.GetMatchDetailAsync(competition.CompetitionID, unplayedMatch.MatchID, you.Id);
            var notFoundForWrongCompetition = await service.GetMatchDetailAsync(Guid.NewGuid(), match.MatchID, you.Id);

            detail.Should().NotBeNull();
            detail!.MatchID.Should().Be(match.MatchID);
            detail.HomeTeamID.Should().Be(home.TeamID);
            detail.AwayTeamID.Should().Be(away.TeamID);
            detail.YourPredictionScore.Should().Be(3);
            detail.AveragePredictionScore.Should().Be(2m);

            notFoundForUnplayedMatch.Should().BeNull();
            notFoundForWrongCompetition.Should().BeNull();
        }
        finally
        {
            await CleanUpAsync(dbContext,
                [competition.CompetitionID],
                [match.MatchID, unplayedMatch.MatchID],
                [you.Id, otherUser.Id],
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
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => matchIds.Contains(m.MatchID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        dbContext.Team.RemoveRange(dbContext.Team.Where(t => teamIds.Contains(t.TeamID)));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => competitionIds.Contains(c.CompetitionID)));
        await dbContext.SaveChangesAsync();
    }
}
