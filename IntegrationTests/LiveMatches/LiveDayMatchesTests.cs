using FluentAssertions;
using MapsterMapper;
using Predictathon.Application.Common;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;

namespace Predictathon.IntegrationTests.LiveMatches;

/// <summary>
/// Exercises MatchService.GetLiveDayMatchesAsync against the real dbo.UserMatchPredictionListGet -
/// the date window it passes, the post-filter that only carries pre-midnight matches over while
/// they're unresolved, and the MatchPlayed column the Live updates section leans on to tell "in
/// play" apart from "finished". None of that is meaningfully testable through the unit tests'
/// InMemory fake, whose stored-procedure calls return nothing.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class LiveDayMatchesTests
{
    private readonly DatabaseFixture _fixture;

    public LiveDayMatchesTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static MatchService MakeService(ApplicationDbContext dbContext)
    {
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateMatchModel, MatchModel>(dbContext, new Mapper());
        return new MatchService(dependencyAggregate, dbContext, dbContext, new LeagueDataCache());
    }

    [Fact]
    public async Task GetLiveDayMatchesAsync_ReturnsTodaysMatchesWithTheUsersOwnPredictionAndResultState()
    {
        await using var dbContext = _fixture.CreateDbContext();

        // Matches are stored as naive UK wall-clock times, so the window the service asks for is
        // built from UK "now" - anchor the fixtures to the same clock.
        var now = UkClock.Now;

        var competition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(now.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(now.AddDays(30)),
        };
        var otherCompetition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(now.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(now.AddDays(30)),
        };
        dbContext.Competition.AddRange(competition, otherCompetition);

        var you = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"you-{Guid.NewGuid():N}" };
        var otherUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"other-{Guid.NewGuid():N}" };
        dbContext.Users.AddRange(you, otherUser);

        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "AWY" };
        dbContext.Team.AddRange(home, away);

        // Pinned to fixed points inside today rather than offsets from `now`, so the test can't
        // straddle midnight and land a "today" fixture on either side of it.
        var startOfToday = now.Date;
        var liveMatch = MakeMatch(competition, home, away, startOfToday.AddHours(13), matchPlayed: false);
        var completedMatch = MakeMatch(competition, home, away, startOfToday.AddHours(11), matchPlayed: true, homeGoals: 2, awayGoals: 1);
        var laterTodayMatch = MakeMatch(competition, home, away, startOfToday.AddHours(23).AddMinutes(59), matchPlayed: false);
        var tomorrowMatch = MakeMatch(competition, home, away, startOfToday.AddDays(1).AddHours(15), matchPlayed: false);
        var otherCompetitionMatch = MakeMatch(otherCompetition, home, away, startOfToday.AddHours(13), matchPlayed: false);

        // Yesterday, in the two shapes that are excluded whatever time of day this test runs: a
        // confirmed result belongs on the Results page rather than today's card, and an unresolved
        // fixture from lunchtime is long past the carry-over. The carry-over's own boundary depends
        // on the clock (it only reaches back before midnight in the small hours), so it's pinned
        // down deterministically in LiveDayWindowTests rather than here.
        var settledLastNightMatch = MakeMatch(competition, home, away, startOfToday.AddHours(-1), matchPlayed: true, homeGoals: 0, awayGoals: 0);
        var staleUnresolvedMatch = MakeMatch(competition, home, away, startOfToday.AddHours(-12), matchPlayed: false);

        dbContext.Match.AddRange(liveMatch, completedMatch, laterTodayMatch, tomorrowMatch, otherCompetitionMatch,
            settledLastNightMatch, staleUnresolvedMatch);

        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = liveMatch.MatchID, UserID = you.Id, HomeTeamGoals = 3, AwayTeamGoals = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = liveMatch.MatchID, UserID = otherUser.Id, HomeTeamGoals = 1, AwayTeamGoals = 1 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = completedMatch.MatchID, UserID = you.Id, HomeTeamGoals = 2, AwayTeamGoals = 1, Score = 3, GoalDifference = 0 });

        await dbContext.SaveChangesAsync();

        try
        {
            var service = MakeService(dbContext);

            var matches = await service.GetLiveDayMatchesAsync(you.Id, competition.CompetitionID);

            matches.Select(m => m.MatchID).Should().NotContain(
                [tomorrowMatch.MatchID, otherCompetitionMatch.MatchID, settledLastNightMatch.MatchID, staleUnresolvedMatch.MatchID]);

            var live = matches.Single(m => m.MatchID == liveMatch.MatchID);
            live.MatchPlayed.Should().BeFalse();
            live.ActualHomeTeamGoals.Should().BeNull();
            live.HomeTeamGoals.Should().Be(3, "the list carries the caller's own prediction, not another user's");
            live.AwayTeamGoals.Should().Be(0);

            var completed = matches.Single(m => m.MatchID == completedMatch.MatchID);
            completed.MatchPlayed.Should().BeTrue();
            completed.ActualHomeTeamGoals.Should().Be(2);
            completed.ActualAwayTeamGoals.Should().Be(1);
            completed.Score.Should().Be(3);

            matches.Should().Contain(m => m.MatchID == laterTodayMatch.MatchID,
                "the window runs to the last second of today, not to 'now'");

            matches.Select(m => m.MatchDateTime).Should().BeInAscendingOrder();
        }
        finally
        {
            await CleanUpAsync(dbContext,
                [competition.CompetitionID, otherCompetition.CompetitionID],
                [liveMatch.MatchID, completedMatch.MatchID, laterTodayMatch.MatchID, tomorrowMatch.MatchID,
                    otherCompetitionMatch.MatchID, settledLastNightMatch.MatchID, staleUnresolvedMatch.MatchID],
                [you.Id, otherUser.Id],
                [home.TeamID, away.TeamID]);
        }
    }

    private static Match MakeMatch(
        Competition competition,
        Team home,
        Team away,
        DateTime kickoff,
        bool matchPlayed,
        int? homeGoals = null,
        int? awayGoals = null)
    {
        return new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = kickoff,
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = matchPlayed,
            HomeTeamGoals = homeGoals,
            AwayTeamGoals = awayGoals,
        };
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
