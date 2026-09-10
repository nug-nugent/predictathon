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

namespace Predictathon.IntegrationTests.MatchResults;

/// <summary>
/// Exercises the auto-processing sweep against real SQL Server, end to end: the query that finds a
/// finished-but-unprocessed match (a left join onto dbo.MatchLiveScore, which the unit tests' EF
/// InMemory fake can't vouch for), the confirmed result it writes, and dbo.MatchPredictionScoreSet
/// running behind it so the match's predictions come out scored.
///
/// The sweep is deliberately competition-wide - it has no competition to be scoped to - so these
/// tests assert on their own match rather than on the totals the sweep reports.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class MatchResultAutoProcessTests
{
    private readonly DatabaseFixture _fixture;

    public MatchResultAutoProcessTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static MatchResultAutoProcessService MakeService(ApplicationDbContext dbContext)
    {
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateMatchModel, MatchModel>(dbContext, new Mapper());
        var matchService = new MatchService(dependencyAggregate, dbContext, dbContext, new LeagueDataCache());

        return new MatchResultAutoProcessService(
            dbContext,
            matchService,
            Options.Create(new FootballDataApiOptions()),
            NullLogger<MatchResultAutoProcessService>.Instance);
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_ConfirmsTheResultAndScoresThePredictions_ForAFinishedMatch()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = await CreateCompetitionAsync(dbContext);
        var (homeTeamId, awayTeamId) = await CreateTeamsAsync(dbContext);

        var match = NewMatch(competition.CompetitionID, homeTeamId, awayTeamId);
        dbContext.Match.Add(match);
        dbContext.MatchLiveScore.Add(NewLiveScore(match.MatchID, 2, 1, ExternalMatchScore.FinishedStatus));

        var perfectUser = await CreateUserAsync(dbContext);
        var wrongUser = await CreateUserAsync(dbContext);
        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = perfectUser.Id, HomeTeamGoals = 2, AwayTeamGoals = 1 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = match.MatchID, UserID = wrongUser.Id, HomeTeamGoals = 0, AwayTeamGoals = 2 });

        await dbContext.SaveChangesAsync();

        try
        {
            await MakeService(dbContext).ProcessFinishedMatchesAsync();

            var processed = await dbContext.Match.AsNoTracking().SingleAsync(m => m.MatchID == match.MatchID);
            processed.MatchPlayed.Should().BeTrue();
            processed.HomeTeamGoals.Should().Be(2);
            processed.AwayTeamGoals.Should().Be(1);

            var scores = await dbContext.Prediction
                .AsNoTracking()
                .Where(p => p.MatchID == match.MatchID)
                .ToDictionaryAsync(p => p.UserID, p => p.Score);

            scores[perfectUser.Id].Should().Be(3, "confirming a result automatically should score its predictions too, not just write the match's goals");
            scores[wrongUser.Id].Should().Be(0);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [perfectUser.Id, wrongUser.Id], [homeTeamId, awayTeamId]);
        }
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_LeavesAMatchUnprocessed_WhileTheProviderStillHasItInPlay()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = await CreateCompetitionAsync(dbContext);
        var (homeTeamId, awayTeamId) = await CreateTeamsAsync(dbContext);

        var match = NewMatch(competition.CompetitionID, homeTeamId, awayTeamId);
        dbContext.Match.Add(match);
        dbContext.MatchLiveScore.Add(NewLiveScore(match.MatchID, 1, 0, "IN_PLAY"));

        await dbContext.SaveChangesAsync();

        try
        {
            await MakeService(dbContext).ProcessFinishedMatchesAsync();

            var untouched = await dbContext.Match.AsNoTracking().SingleAsync(m => m.MatchID == match.MatchID);
            untouched.MatchPlayed.Should().BeFalse();
            untouched.HomeTeamGoals.Should().BeNull();
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [], [homeTeamId, awayTeamId]);
        }
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_LeavesAMatchUnprocessed_WhenNobodyHasALiveScoreForIt()
    {
        // The left join's other side: a match with no dbo.MatchLiveScore row at all mustn't be swept
        // up, which is the case a NOT NULL comparison in the wrong direction would get wrong.
        await using var dbContext = _fixture.CreateDbContext();

        var competition = await CreateCompetitionAsync(dbContext);
        var (homeTeamId, awayTeamId) = await CreateTeamsAsync(dbContext);

        var match = NewMatch(competition.CompetitionID, homeTeamId, awayTeamId);
        dbContext.Match.Add(match);

        await dbContext.SaveChangesAsync();

        try
        {
            await MakeService(dbContext).ProcessFinishedMatchesAsync();

            var untouched = await dbContext.Match.AsNoTracking().SingleAsync(m => m.MatchID == match.MatchID);
            untouched.MatchPlayed.Should().BeFalse();
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [], [homeTeamId, awayTeamId]);
        }
    }

    /// <summary>
    /// A match that kicked off three hours ago, so it's comfortably past the point its result can be
    /// confirmed, with no result of its own yet.
    /// </summary>
    /// <param name="competitionId">The competition the match belongs to.</param>
    /// <param name="homeTeamId">The home team.</param>
    /// <param name="awayTeamId">The away team.</param>
    private static Match NewMatch(Guid competitionId, Guid homeTeamId, Guid awayTeamId)
    {
        return new Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competitionId,
            MatchDateTime = UkClock.Now.AddHours(-3),
            HomeTeamID = homeTeamId,
            AwayTeamID = awayTeamId,
            MatchPlayed = false,
        };
    }

    /// <summary>
    /// The provider's score for a match, as the poller would have stored it.
    /// </summary>
    /// <param name="matchId">The match the score belongs to.</param>
    /// <param name="homeTeamGoals">Home goals.</param>
    /// <param name="awayTeamGoals">Away goals.</param>
    /// <param name="status">The provider's status for the match.</param>
    private static MatchLiveScore NewLiveScore(Guid matchId, int homeTeamGoals, int awayTeamGoals, string status)
    {
        var now = UkClock.Now;

        return new MatchLiveScore
        {
            MatchID = matchId,
            HomeTeamGoals = homeTeamGoals,
            AwayTeamGoals = awayTeamGoals,
            Status = status,
            Source = LiveScoreSource.Api,
            UpdatedDateTime = now,
            LastPolledDateTime = now,
        };
    }

    private static async Task<Competition> CreateCompetitionAsync(ApplicationDbContext dbContext)
    {
        var competition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        };
        dbContext.Competition.Add(competition);
        await dbContext.SaveChangesAsync();
        return competition;
    }

    private static async Task<(Guid HomeTeamId, Guid AwayTeamId)> CreateTeamsAsync(ApplicationDbContext dbContext)
    {
        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "AWY" };
        dbContext.Team.AddRange(home, away);
        await dbContext.SaveChangesAsync();
        return (home.TeamID, away.TeamID);
    }

    private static async Task<ApplicationUser> CreateUserAsync(ApplicationDbContext dbContext)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"integration-{Guid.NewGuid():N}" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static async Task CleanUpAsync(
        ApplicationDbContext dbContext,
        Guid competitionId,
        IReadOnlyList<Guid> matchIds,
        IReadOnlyList<Guid> userIds,
        IReadOnlyList<Guid> teamIds)
    {
        dbContext.MatchLiveScore.RemoveRange(dbContext.MatchLiveScore.Where(s => matchIds.Contains(s.MatchID)));
        dbContext.Prediction.RemoveRange(dbContext.Prediction.Where(p => matchIds.Contains(p.MatchID)));
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => matchIds.Contains(m.MatchID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        dbContext.Team.RemoveRange(dbContext.Team.Where(t => teamIds.Contains(t.TeamID)));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => c.CompetitionID == competitionId));
        await dbContext.SaveChangesAsync();
    }
}
