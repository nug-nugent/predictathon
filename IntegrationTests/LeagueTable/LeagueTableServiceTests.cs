using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;
using Predictathon.IntegrationTests.TestDoubles;

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
            var service = new LeagueTableService(dbContext, new StubAvatarService(), new LeagueTableCache());

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

    [Fact]
    public async Task GetLeagueTableAsync_PopulatesPreviousLeaguePosition_FromHistorySnapshotBeforeComparisonDate()
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
        dbContext.Users.AddRange(leader, runnerUp);

        var leaderRegistration = new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = leader.Id, CompetitionID = competition.CompetitionID };
        var runnerUpRegistration = new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = runnerUp.Id, CompetitionID = competition.CompetitionID };
        dbContext.UserCompetition.AddRange(leaderRegistration, runnerUpRegistration);

        // UserCompetitionLeagueHistory.UserCompetitionID is a plain FK column with no modelled EF
        // navigation back to UserCompetition, so EF's change tracker has no dependency graph edge
        // telling it to insert UserCompetition rows first - saving here guarantees that ordering
        // instead of relying on SaveChangesAsync to infer it later.
        await dbContext.SaveChangesAsync();

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
            AwayTeamGoals = 0,
        };
        dbContext.Match.Add(match);

        // Leader is ahead today (a 3-pointer vs a 1-pointer), so should rank 1st.
        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = leader.Id, HomeTeamGoals = 2, AwayTeamGoals = 0, Score = 3, GoalDifference = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = runnerUp.Id, HomeTeamGoals = 1, AwayTeamGoals = 0, Score = 1, GoalDifference = -1 });

        // But yesterday's snapshot had the two swapped - runnerUp was 1st, leader was 2nd.
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        dbContext.UserCompetitionLeagueHistory.AddRange(
            new UserCompetitionLeagueHistory { UserCompetitionLeagueHistoryID = Guid.NewGuid(), UserCompetitionID = leaderRegistration.UserCompetitionID, Date = yesterday, LeaguePosition = 2, Score = 1, AverageGoalDifference = 0, TotalGoalDifference = 0 },
            new UserCompetitionLeagueHistory { UserCompetitionLeagueHistoryID = Guid.NewGuid(), UserCompetitionID = runnerUpRegistration.UserCompetitionID, Date = yesterday, LeaguePosition = 1, Score = 3, AverageGoalDifference = 0, TotalGoalDifference = 0 });

        await dbContext.SaveChangesAsync();

        try
        {
            var service = new LeagueTableService(dbContext, new StubAvatarService(), new LeagueTableCache());
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var table = await service.GetLeagueTableAsync(competition.CompetitionID, dateForComparison: today);

            var leaderRow = table.Single(r => r.UserID == leader.Id);
            var runnerUpRow = table.Single(r => r.UserID == runnerUp.Id);

            leaderRow.LeaguePosition.Should().Be(1);
            leaderRow.PreviousLeaguePosition.Should().Be(2);

            runnerUpRow.LeaguePosition.Should().Be(2);
            runnerUpRow.PreviousLeaguePosition.Should().Be(1);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID,
                [match.MatchID],
                [leader.Id, runnerUp.Id],
                [home.TeamID, away.TeamID]);
        }
    }

    [Fact]
    public async Task GetLeagueTableAsync_LeavesPreviousLeaguePositionNull_WhenNoComparisonDateSupplied()
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
        dbContext.UserCompetition.Add(new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = user.Id, CompetitionID = competition.CompetitionID });

        await dbContext.SaveChangesAsync();

        try
        {
            var service = new LeagueTableService(dbContext, new StubAvatarService(), new LeagueTableCache());

            var table = await service.GetLeagueTableAsync(competition.CompetitionID);

            table.Single(r => r.UserID == user.Id).PreviousLeaguePosition.Should().BeNull();
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [], [user.Id], []);
        }
    }

    [Fact]
    public async Task GetLeagueTableAsync_PopulatesAvatarUrl_OnlyForUsersWithAnUploadedImage()
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

        var withAvatar = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"with-avatar-{Guid.NewGuid():N}", ImageUploaded = true };
        var withoutAvatar = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"without-avatar-{Guid.NewGuid():N}", ImageUploaded = false };
        dbContext.Users.AddRange(withAvatar, withoutAvatar);

        dbContext.UserCompetition.AddRange(
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = withAvatar.Id, CompetitionID = competition.CompetitionID },
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = withoutAvatar.Id, CompetitionID = competition.CompetitionID });

        await dbContext.SaveChangesAsync();

        try
        {
            var service = new LeagueTableService(dbContext, new StubAvatarService(), new LeagueTableCache());

            var table = await service.GetLeagueTableAsync(competition.CompetitionID);

            // The procedure returns ImageUploaded per user; the service turns it into a URL, so
            // clients can render an avatar beside a player without a lookup per row.
            table.Single(r => r.UserID == withAvatar.Id).AvatarUrl.Should().Be(StubAvatarService.AvatarUrl);
            table.Single(r => r.UserID == withoutAvatar.Id).AvatarUrl.Should().BeNull();
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [], [withAvatar.Id, withoutAvatar.Id], []);
        }
    }

    private static async Task CleanUpAsync(
        ApplicationDbContext dbContext,
        Guid competitionId,
        IReadOnlyList<Guid> matchIds,
        IReadOnlyList<Guid> userIds,
        IReadOnlyList<Guid> teamIds)
    {
        var userCompetitionIds = await dbContext.UserCompetition
            .Where(uc => uc.CompetitionID == competitionId)
            .Select(uc => uc.UserCompetitionID)
            .ToListAsync();

        // UserCompetitionLeagueHistory has no modelled EF navigation to UserCompetition (see the
        // matching comment in the arrange step above), so it has to be deleted - and saved - before
        // UserCompetition, or EF's unaware-of-the-FK ordering can send the DELETEs the wrong way round.
        dbContext.UserCompetitionLeagueHistory.RemoveRange(dbContext.UserCompetitionLeagueHistory.Where(h => userCompetitionIds.Contains(h.UserCompetitionID)));
        await dbContext.SaveChangesAsync();

        dbContext.Prediction.RemoveRange(dbContext.Prediction.Where(p => matchIds.Contains(p.MatchID)));
        dbContext.UserCompetition.RemoveRange(dbContext.UserCompetition.Where(uc => uc.CompetitionID == competitionId));
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => matchIds.Contains(m.MatchID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        dbContext.Team.RemoveRange(dbContext.Team.Where(t => teamIds.Contains(t.TeamID)));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => c.CompetitionID == competitionId));
        await dbContext.SaveChangesAsync();
    }
}
