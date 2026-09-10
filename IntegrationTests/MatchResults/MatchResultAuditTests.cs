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
/// Covers the audit trail dbo.Match keeps of how a result was confirmed - ProcessedDateTime and
/// ProcessedByUserID. Here rather than in the unit tests because ProcessedByUserID is a real
/// foreign key onto Identity.Users, so the two ways a result arrives (an admin on the Process
/// Results page, and the auto-processor working from the provider's final score) can only be told
/// apart against a database that enforces it.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class MatchResultAuditTests
{
    private readonly DatabaseFixture _fixture;

    public MatchResultAuditTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static MatchService MakeMatchService(ApplicationDbContext dbContext)
    {
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateMatchModel, MatchModel>(dbContext, new Mapper());
        return new MatchService(dependencyAggregate, dbContext, dbContext, new LeagueDataCache());
    }

    [Fact]
    public async Task SaveResultAsync_RecordsWhenAndWhoConfirmedIt_WhenAnAdminEntersTheResult()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var competition = await CreateCompetitionAsync(dbContext);
        var (homeTeamId, awayTeamId) = await CreateTeamsAsync(dbContext);
        var admin = await CreateUserAsync(dbContext);

        var match = NewMatch(competition.CompetitionID, homeTeamId, awayTeamId);
        dbContext.Match.Add(match);
        await dbContext.SaveChangesAsync();

        try
        {
            var before = UkClock.Now.AddSeconds(-1);

            var result = await MakeMatchService(dbContext)
                .SaveResultAsync(match.MatchID, homeTeamGoals: 2, awayTeamGoals: 1, processedByUserId: admin.Id);

            result.IsSuccess.Should().BeTrue();

            var processed = await dbContext.Match.AsNoTracking().SingleAsync(m => m.MatchID == match.MatchID);
            processed.ProcessedByUserID.Should().Be(admin.Id);
            processed.ProcessedDateTime.Should().NotBeNull().And.BeAfter(before);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [admin.Id], [homeTeamId, awayTeamId]);
        }
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_RecordsWhenItWasConfirmedButNoUser_WhenTheProvidersScoreConfirmsIt()
    {
        // The distinction the columns exist to draw: a set ProcessedDateTime with a null
        // ProcessedByUserID is what says no admin was involved in this result.
        await using var dbContext = _fixture.CreateDbContext();

        var competition = await CreateCompetitionAsync(dbContext);
        var (homeTeamId, awayTeamId) = await CreateTeamsAsync(dbContext);

        var match = NewMatch(competition.CompetitionID, homeTeamId, awayTeamId);
        dbContext.Match.Add(match);
        dbContext.MatchLiveScore.Add(new MatchLiveScore
        {
            MatchID = match.MatchID,
            HomeTeamGoals = 3,
            AwayTeamGoals = 0,
            Status = ExternalMatchScore.FinishedStatus,
            Source = LiveScoreSource.Api,
            UpdatedDateTime = UkClock.Now,
            LastPolledDateTime = UkClock.Now,
        });
        await dbContext.SaveChangesAsync();

        try
        {
            var before = UkClock.Now.AddSeconds(-1);

            var autoProcessService = new MatchResultAutoProcessService(
                dbContext,
                MakeMatchService(dbContext),
                Options.Create(new FootballDataApiOptions()),
                NullLogger<MatchResultAutoProcessService>.Instance);

            await autoProcessService.ProcessFinishedMatchesAsync();

            var processed = await dbContext.Match.AsNoTracking().SingleAsync(m => m.MatchID == match.MatchID);
            processed.MatchPlayed.Should().BeTrue();
            processed.ProcessedByUserID.Should().BeNull("nobody confirmed this by hand");
            processed.ProcessedDateTime.Should().NotBeNull().And.BeAfter(before);
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [], [homeTeamId, awayTeamId]);
        }
    }

    [Fact]
    public async Task Update_LeavesTheAuditTrailAlone_WhenAnAdminCorrectsAnAutoProcessedResult()
    {
        // The columns say how the result got here, not who last edited it - so a correction on top
        // of an automatic result must not start claiming an admin confirmed it.
        await using var dbContext = _fixture.CreateDbContext();

        var competition = await CreateCompetitionAsync(dbContext);
        var (homeTeamId, awayTeamId) = await CreateTeamsAsync(dbContext);

        var processedAt = UkClock.Now.AddHours(-1);
        var match = NewMatch(competition.CompetitionID, homeTeamId, awayTeamId);
        match.MatchPlayed = true;
        match.HomeTeamGoals = 1;
        match.AwayTeamGoals = 1;
        match.ProcessedDateTime = processedAt;
        match.ProcessedByUserID = null;
        dbContext.Match.Add(match);
        await dbContext.SaveChangesAsync();

        try
        {
            var corrected = new MatchModel
            {
                MatchID = match.MatchID,
                CompetitionID = competition.CompetitionID,
                MatchDateTime = match.MatchDateTime,
                HomeTeamID = homeTeamId,
                AwayTeamID = awayTeamId,
                HomeTeamGoals = 2,
                AwayTeamGoals = 1,
                MatchPlayed = true,
            };

            var result = await MakeMatchService(dbContext).Update(match.MatchID, corrected);

            result.IsSuccess.Should().BeTrue();

            var updated = await dbContext.Match.AsNoTracking().SingleAsync(m => m.MatchID == match.MatchID);
            updated.HomeTeamGoals.Should().Be(2, "the correction itself should still land");
            updated.ProcessedByUserID.Should().BeNull();
            updated.ProcessedDateTime.Should().BeCloseTo(processedAt, TimeSpan.FromSeconds(1),
                "MatchModel carries neither column, so mapping a corrected model over the entity leaves them untouched");
        }
        finally
        {
            await CleanUpAsync(dbContext, competition.CompetitionID, [match.MatchID], [], [homeTeamId, awayTeamId]);
        }
    }

    /// <summary>
    /// A match that kicked off three hours ago, so its result is eligible to be confirmed, with no
    /// result of its own yet.
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
