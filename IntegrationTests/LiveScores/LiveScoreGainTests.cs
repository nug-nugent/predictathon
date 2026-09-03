using FluentAssertions;
using Predictathon.Application.Common;
using Predictathon.Application.Constants;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;
using Predictathon.IntegrationTests.TestDoubles;

namespace Predictathon.IntegrationTests.LiveScores;

/// <summary>
/// Exercises dbo.LiveLeagueTableGet, the Live page's league table: the real table with the live
/// scores applied, alongside where each user stands on confirmed results alone. Three things have to
/// hold and none is checkable outside a real database - its live scoring CASE has to agree with
/// MatchPredictionScoreSet's, or the table shows points that never arrive; both of its orderings
/// have to separate equal totals the way LeagueTableGet does, or it disagrees with the League page;
/// and the two together have to make the position-change arrow mean what it means everywhere else.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class LiveScoreGainTests
{
    private readonly DatabaseFixture _fixture;

    public LiveScoreGainTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // A cache of its own per service, so the brief reuse window the Live page relies on can't
    // carry one test's table into another's assertions - these tests each set up their own
    // competition and ask for its table once.
    private static LeagueTableService MakeService(ApplicationDbContext dbContext)
        => new(dbContext, new StubAvatarService(), new LeagueDataCache());

    [Theory]
    // A perfect prediction is three, whatever the competition allows.
    [InlineData(2, 1, 2, 1, true, 3)]
    [InlineData(2, 1, 2, 1, false, 3)]
    // Right result, right goals for the winning side: two where the competition allows it, one
    // where it doesn't.
    [InlineData(2, 1, 2, 0, true, 2)]
    [InlineData(2, 1, 2, 0, false, 1)]
    // Right result, wrong goals either side.
    [InlineData(2, 1, 3, 0, true, 1)]
    // Away win, same rules mirrored.
    [InlineData(1, 2, 0, 2, true, 2)]
    [InlineData(1, 2, 0, 3, true, 1)]
    // A draw predicted as a different draw is one; a perfect draw is three.
    [InlineData(1, 1, 2, 2, true, 1)]
    [InlineData(1, 1, 1, 1, true, 3)]
    // Wrong result entirely.
    [InlineData(2, 1, 0, 1, true, 0)]
    [InlineData(1, 1, 2, 0, true, 0)]
    public async Task GetLiveLeagueTableAsync_ScoresALivePredictionTheSameWayAConfirmedOneWouldBe(
        int liveHome,
        int liveAway,
        int predictedHome,
        int predictedAway,
        bool allowTwoPointers,
        int expectedLivePoints)
    {
        await using var dbContext = _fixture.CreateDbContext();

        var now = UkClock.Now;
        var competition = NewCompetition(now, allowTwoPointers);
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"user-{Guid.NewGuid():N}" };
        var match = NewMatch(competition, now.AddMinutes(-30));

        dbContext.Competition.Add(competition);
        dbContext.Users.Add(user);
        dbContext.Match.Add(match);
        dbContext.UserCompetition.Add(new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = user.Id, CompetitionID = competition.CompetitionID });
        dbContext.MatchLiveScore.Add(NewLiveScore(match.MatchID, liveHome, liveAway, now));
        dbContext.Prediction.Add(new Prediction
        {
            PredictionID = Guid.NewGuid(),
            MatchID = match.MatchID,
            UserID = user.Id,
            HomeTeamGoals = predictedHome,
            AwayTeamGoals = predictedAway,
        });

        await dbContext.SaveChangesAsync();

        try
        {
            var table = await MakeService(dbContext).GetLiveLeagueTableAsync(competition.CompetitionID);

            table.Single(r => r.UserID == user.Id).LivePoints.Should().Be(expectedLivePoints);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition, [match], [user]);
        }
    }

    [Fact]
    public async Task GetLiveLeagueTableAsync_CountsEveryMatchInPlay_AndIgnoresOnesThatAreNot()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var now = UkClock.Now;
        var competition = NewCompetition(now, allowTwoPointers: true);
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"user-{Guid.NewGuid():N}" };

        var firstLive = NewMatch(competition, now.AddMinutes(-60));
        var secondLive = NewMatch(competition, now.AddMinutes(-30));
        var noLiveScore = NewMatch(competition, now.AddMinutes(-10));
        var alreadyConfirmed = NewMatch(competition, now.AddHours(-3));
        alreadyConfirmed.MatchPlayed = true;
        alreadyConfirmed.HomeTeamGoals = 1;
        alreadyConfirmed.AwayTeamGoals = 0;

        dbContext.Competition.Add(competition);
        dbContext.Users.Add(user);
        dbContext.Match.AddRange(firstLive, secondLive, noLiveScore, alreadyConfirmed);
        dbContext.UserCompetition.Add(new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = user.Id, CompetitionID = competition.CompetitionID });

        dbContext.MatchLiveScore.AddRange(
            NewLiveScore(firstLive.MatchID, 1, 0, now),
            NewLiveScore(secondLive.MatchID, 2, 2, now),
            // A confirmed result keeps its live-score row, but its points are already in the
            // standings - counting them here as well would show them twice.
            NewLiveScore(alreadyConfirmed.MatchID, 1, 0, now));

        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = firstLive.MatchID, UserID = user.Id, HomeTeamGoals = 1, AwayTeamGoals = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = secondLive.MatchID, UserID = user.Id, HomeTeamGoals = 1, AwayTeamGoals = 1 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = noLiveScore.MatchID, UserID = user.Id, HomeTeamGoals = 3, AwayTeamGoals = 3 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = alreadyConfirmed.MatchID, UserID = user.Id, HomeTeamGoals = 1, AwayTeamGoals = 0 });

        await dbContext.SaveChangesAsync();

        try
        {
            var table = await MakeService(dbContext).GetLiveLeagueTableAsync(competition.CompetitionID);

            // 3 for the exact 1-0, plus 1 for calling the draw but not the score. The match with no
            // live score and the confirmed one contribute nothing.
            table.Single(r => r.UserID == user.Id).LivePoints.Should().Be(4);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition, [firstLive, secondLive, noLiveScore, alreadyConfirmed], [user]);
        }
    }

    [Fact]
    public async Task GetLiveLeagueTableAsync_ReportsNoGain_ForSomeoneWhoDidNotPredict()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var now = UkClock.Now;
        var competition = NewCompetition(now, allowTwoPointers: true);
        var predictor = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"predictor-{Guid.NewGuid():N}" };
        var absentee = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"absentee-{Guid.NewGuid():N}" };
        var match = NewMatch(competition, now.AddMinutes(-30));

        dbContext.Competition.Add(competition);
        dbContext.Users.AddRange(predictor, absentee);
        dbContext.Match.Add(match);
        dbContext.UserCompetition.AddRange(
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = predictor.Id, CompetitionID = competition.CompetitionID },
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = absentee.Id, CompetitionID = competition.CompetitionID });
        dbContext.MatchLiveScore.Add(NewLiveScore(match.MatchID, 1, 0, now));
        dbContext.Prediction.Add(new Prediction
        {
            PredictionID = Guid.NewGuid(),
            MatchID = match.MatchID,
            UserID = predictor.Id,
            HomeTeamGoals = 1,
            AwayTeamGoals = 0,
        });

        await dbContext.SaveChangesAsync();

        try
        {
            var table = await MakeService(dbContext).GetLiveLeagueTableAsync(competition.CompetitionID);

            // The procedure returns no row at all for the absentee; the service reads that as a gain
            // of nothing rather than dropping them out of the standings.
            table.Should().HaveCount(2);
            table.Single(r => r.UserID == predictor.Id).LivePoints.Should().Be(3);
            table.Single(r => r.UserID == absentee.Id).LivePoints.Should().Be(0);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition, [match], [predictor, absentee]);
        }
    }

    [Fact]
    public async Task GetLiveLeagueTableAsync_SeparatesEqualProjectedPoints_OnGoalDifference()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var now = UkClock.Now;
        var competition = NewCompetition(now, allowTwoPointers: true);

        // Usernames chosen to sort the opposite way to goal difference: with nothing played, the two
        // are level on every confirmed measure and the table falls back to username, so "aaa" starts
        // ahead. The projection has to put "zzz" ahead on goal difference instead - if it only
        // looked at points, both would stay where they were and this would fail.
        var closer = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"zzz-{Guid.NewGuid():N}" };
        var further = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"aaa-{Guid.NewGuid():N}" };
        var match = NewMatch(competition, now.AddMinutes(-30));

        dbContext.Competition.Add(competition);
        dbContext.Users.AddRange(closer, further);
        dbContext.Match.Add(match);
        dbContext.UserCompetition.AddRange(
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = closer.Id, CompetitionID = competition.CompetitionID },
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = further.Id, CompetitionID = competition.CompetitionID });
        dbContext.MatchLiveScore.Add(NewLiveScore(match.MatchID, 2, 1, now));

        // Both call the home win without getting the goals right, so both gain a single point;
        // 3-0 is two goals away from 2-1, and 4-0 is three.
        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = closer.Id, HomeTeamGoals = 3, AwayTeamGoals = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = further.Id, HomeTeamGoals = 4, AwayTeamGoals = 0 });

        await dbContext.SaveChangesAsync();

        try
        {
            var table = await MakeService(dbContext).GetLiveLeagueTableAsync(competition.CompetitionID);

            var closerRow = table.Single(r => r.UserID == closer.Id);
            var furtherRow = table.Single(r => r.UserID == further.Id);

            closerRow.LivePoints.Should().Be(1);
            furtherRow.LivePoints.Should().Be(1);

            furtherRow.PreviousLeaguePosition.Should().BeLessThan(closerRow.PreviousLeaguePosition!.Value,
                "nothing is played, so the confirmed table falls back to usernames");
            closerRow.LeaguePosition.Should().BeLessThan(furtherRow.LeaguePosition,
                "with the live scores applied the closer prediction's goal difference puts it ahead");
        }
        finally
        {
            await CleanUpAsync(dbContext, competition, [match], [closer, further]);
        }
    }

    [Fact]
    public async Task GetLiveLeagueTableAsync_NeverShowsTheLeaderFalling()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var now = UkClock.Now;
        var competition = NewCompetition(now, allowTwoPointers: true);
        var leader = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"aaa-{Guid.NewGuid():N}" };
        var chaser = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"bbb-{Guid.NewGuid():N}" };
        var match = NewMatch(competition, now.AddMinutes(-30));

        dbContext.Competition.Add(competition);
        dbContext.Users.AddRange(leader, chaser);
        dbContext.Match.Add(match);
        dbContext.UserCompetition.AddRange(
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = leader.Id, CompetitionID = competition.CompetitionID },
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = chaser.Id, CompetitionID = competition.CompetitionID });
        dbContext.MatchLiveScore.Add(NewLiveScore(match.MatchID, 1, 0, now));

        // The chaser nails it and the leader gets nothing, so the live table swaps them over.
        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = leader.Id, HomeTeamGoals = 0, AwayTeamGoals = 3 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = chaser.Id, HomeTeamGoals = 1, AwayTeamGoals = 0 });

        await dbContext.SaveChangesAsync();

        try
        {
            var table = await MakeService(dbContext).GetLiveLeagueTableAsync(competition.CompetitionID);

            // Whoever ends up on top got there by climbing or by holding on. A row can't be shown
            // falling out of the position it currently occupies - which is the whole reason the
            // table is the live one and the arrow measures the move into it.
            var top = table.Single(r => r.LeaguePosition == 1);
            top.UserID.Should().Be(chaser.Id);
            top.PreviousLeaguePosition.Should().NotBeNull().And.BeGreaterThanOrEqualTo(top.LeaguePosition);

            table.Single(r => r.UserID == leader.Id).LeaguePosition.Should().Be(2, "the leader gained nothing and was overtaken");
        }
        finally
        {
            await CleanUpAsync(dbContext, competition, [match], [leader, chaser]);
        }
    }

    [Fact]
    public async Task GetLiveLeagueTableAsync_ProjectsNoMovement_WhenNothingIsInPlay()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var now = UkClock.Now;
        var competition = NewCompetition(now, allowTwoPointers: true);
        var first = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"aaa-{Guid.NewGuid():N}" };
        var second = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"bbb-{Guid.NewGuid():N}" };

        dbContext.Competition.Add(competition);
        dbContext.Users.AddRange(first, second);
        dbContext.UserCompetition.AddRange(
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = first.Id, CompetitionID = competition.CompetitionID },
            new UserCompetition { UserCompetitionID = Guid.NewGuid(), UserID = second.Id, CompetitionID = competition.CompetitionID });

        await dbContext.SaveChangesAsync();

        try
        {
            var table = await MakeService(dbContext).GetLiveLeagueTableAsync(competition.CompetitionID);

            table.Should().OnlyContain(r => r.LivePoints == 0);
            table.Should().OnlyContain(r => r.PreviousLeaguePosition == r.LeaguePosition,
                "with nothing in play the live table is the confirmed table, and every arrow should read as no change");
        }
        finally
        {
            await CleanUpAsync(dbContext, competition, [], [first, second]);
        }
    }

    private static Competition NewCompetition(DateTime now, bool allowTwoPointers) => new()
    {
        CompetitionID = Guid.NewGuid(),
        CompetitionName = $"Integration Test {Guid.NewGuid():N}",
        StartDate = DateOnly.FromDateTime(now.AddDays(-30)),
        EndDate = DateOnly.FromDateTime(now.AddDays(30)),
        AllowTwoPointers = allowTwoPointers,
    };

    private static Match NewMatch(Competition competition, DateTime kickoff) => new()
    {
        MatchID = Guid.NewGuid(),
        CompetitionID = competition.CompetitionID,
        MatchDateTime = kickoff,
        MatchPlayed = false,
    };

    private static MatchLiveScore NewLiveScore(Guid matchId, int home, int away, DateTime now) => new()
    {
        MatchID = matchId,
        HomeTeamGoals = home,
        AwayTeamGoals = away,
        Status = "IN_PLAY",
        Source = LiveScoreSource.Api,
        UpdatedDateTime = now,
        LastPolledDateTime = now,
    };

    private static async Task CleanUpAsync(
        ApplicationDbContext dbContext,
        Competition competition,
        IReadOnlyList<Match> matches,
        IReadOnlyList<ApplicationUser> users)
    {
        var matchIds = matches.Select(m => m.MatchID).ToList();
        var userIds = users.Select(u => u.Id).ToList();

        dbContext.Prediction.RemoveRange(dbContext.Prediction.Where(p => matchIds.Contains(p.MatchID)));
        dbContext.MatchLiveScore.RemoveRange(dbContext.MatchLiveScore.Where(s => matchIds.Contains(s.MatchID)));
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => matchIds.Contains(m.MatchID)));
        dbContext.UserCompetition.RemoveRange(dbContext.UserCompetition.Where(uc => userIds.Contains(uc.UserID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => c.CompetitionID == competition.CompetitionID));
        await dbContext.SaveChangesAsync();
    }
}
