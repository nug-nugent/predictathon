using FluentAssertions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Predictathon.Application.Common;
using Predictathon.Application.Constants;
using Predictathon.Application.Models;
using Predictathon.Application.Options;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;
using Predictathon.IntegrationTests.TestDoubles;

namespace Predictathon.IntegrationTests.LiveScores;

/// <summary>
/// Exercises dbo.MatchLiveScore against real SQL Server: the columns
/// dbo.UserMatchPredictionListGet now joins in (a stored procedure, so not reachable from the unit
/// tests' InMemory fake), and the table's own constraints - the shared primary key that enforces one
/// row per match, and the foreign keys behind it.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class LiveScoreStoreTests
{
    private readonly DatabaseFixture _fixture;

    public LiveScoreStoreTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static MatchService MakeMatchService(ApplicationDbContext dbContext)
    {
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateMatchModel, MatchModel>(dbContext, new Mapper());
        return new MatchService(dependencyAggregate, dbContext, dbContext);
    }

    private static LiveScoreService MakeLiveScoreService(ApplicationDbContext dbContext)
    {
        return new LiveScoreService(
            dbContext,
            new StubExternalMatchDataService(),
            Options.Create(new FootballDataApiOptions()),
            NullLogger<LiveScoreService>.Instance);
    }

    [Fact]
    public async Task GetLiveDayMatchesAsync_CarriesTheLiveScoreAlongsideTheUnconfirmedResult()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var now = UkClock.Now;
        var (competition, user, home, away) = await GivenCompetitionAsync(dbContext, now);

        var liveMatch = NewMatch(competition, home, away, now.Date.AddHours(13));
        var unscoredMatch = NewMatch(competition, home, away, now.Date.AddHours(14));
        dbContext.Match.AddRange(liveMatch, unscoredMatch);

        dbContext.MatchLiveScore.Add(new MatchLiveScore
        {
            MatchID = liveMatch.MatchID,
            HomeTeamGoals = 2,
            AwayTeamGoals = 1,
            Status = "IN_PLAY",
            Source = LiveScoreSource.Api,
            // Deliberately apart: a match can go half an hour without a goal while still being
            // polled every minute, and the Live page shows the poll time so a quiet spell doesn't
            // read as a stalled feed.
            UpdatedDateTime = now.AddMinutes(-30),
            LastPolledDateTime = now,
        });

        await dbContext.SaveChangesAsync();

        try
        {
            var matches = await MakeMatchService(dbContext).GetLiveDayMatchesAsync(user.Id, competition.CompetitionID);

            var scored = matches.Single(m => m.MatchID == liveMatch.MatchID);
            scored.LiveHomeTeamGoals.Should().Be(2);
            scored.LiveAwayTeamGoals.Should().Be(1);
            scored.LiveScoreUpdatedDateTime.Should().BeCloseTo(now.AddMinutes(-30), TimeSpan.FromSeconds(1));
            scored.LiveScoreLastPolledDateTime.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));

            // The confirmed-result columns stay empty: a live score carries no scoring weight, and
            // the two must never be conflated.
            scored.ActualHomeTeamGoals.Should().BeNull();
            scored.ActualAwayTeamGoals.Should().BeNull();
            scored.MatchPlayed.Should().BeFalse();

            var unscored = matches.Single(m => m.MatchID == unscoredMatch.MatchID);
            unscored.LiveHomeTeamGoals.Should().BeNull();
            unscored.LiveAwayTeamGoals.Should().BeNull();
            unscored.LiveScoreLastPolledDateTime.Should().BeNull();
        }
        finally
        {
            await CleanUpAsync(dbContext, competition, [liveMatch, unscoredMatch], user, home, away);
        }
    }

    [Fact]
    public async Task SaveAdminScoreAsync_WritesAndThenUpdatesTheSingleRowForAMatch()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var now = UkClock.Now;
        var (competition, user, home, away) = await GivenCompetitionAsync(dbContext, now);

        var match = NewMatch(competition, home, away, now.AddMinutes(-30));
        dbContext.Match.Add(match);
        await dbContext.SaveChangesAsync();

        try
        {
            var service = MakeLiveScoreService(dbContext);

            var created = await service.SaveAdminScoreAsync(match.MatchID, 1, 0, user.Id);
            created.IsSuccess.Should().BeTrue();

            var updated = await service.SaveAdminScoreAsync(match.MatchID, 2, 2, user.Id);
            updated.IsSuccess.Should().BeTrue();

            // A second save updates in place rather than inserting - the primary key on MatchID is
            // what guarantees it, and a real database is the only place that's actually tested.
            var rows = await dbContext.MatchLiveScore.Where(s => s.MatchID == match.MatchID).ToListAsync();
            rows.Should().ContainSingle();
            rows[0].HomeTeamGoals.Should().Be(2);
            rows[0].AwayTeamGoals.Should().Be(2);
            rows[0].Source.Should().Be(LiveScoreSource.Admin);
            rows[0].UpdatedByUserID.Should().Be(user.Id);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition, [match], user, home, away);
        }
    }

    private static async Task<(Competition Competition, ApplicationUser User, Team Home, Team Away)> GivenCompetitionAsync(
        ApplicationDbContext dbContext,
        DateTime now)
    {
        var competition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(now.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(now.AddDays(30)),
        };
        dbContext.Competition.Add(competition);

        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"user-{Guid.NewGuid():N}" };
        dbContext.Users.Add(user);

        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "AWY" };
        dbContext.Team.AddRange(home, away);

        await Task.CompletedTask;

        return (competition, user, home, away);
    }

    private static Match NewMatch(Competition competition, Team home, Team away, DateTime kickoff)
    {
        return new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = kickoff,
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = false,
        };
    }

    private static async Task CleanUpAsync(
        ApplicationDbContext dbContext,
        Competition competition,
        IReadOnlyList<Match> matches,
        ApplicationUser user,
        Team home,
        Team away)
    {
        var matchIds = matches.Select(m => m.MatchID).ToList();

        dbContext.MatchLiveScore.RemoveRange(dbContext.MatchLiveScore.Where(s => matchIds.Contains(s.MatchID)));
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => matchIds.Contains(m.MatchID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => u.Id == user.Id));
        dbContext.Team.RemoveRange(dbContext.Team.Where(t => t.TeamID == home.TeamID || t.TeamID == away.TeamID));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => c.CompetitionID == competition.CompetitionID));
        await dbContext.SaveChangesAsync();
    }
}
