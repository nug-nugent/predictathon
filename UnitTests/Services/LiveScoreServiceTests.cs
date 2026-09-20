using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Predictathon.Application.Common;
using Predictathon.Application.Constants;
using Predictathon.Application.Models;
using Predictathon.Application.Options;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

/// <summary>
/// Covers the rules that decide what the app believes a live score is: which matches get polled at
/// all, when a reported score is taken and when it's ignored, and how an admin's own entry sits
/// alongside the provider's.
/// </summary>
public class LiveScoreServiceTests
{
    private const string CompetitionCode = "PL";
    private const int PollSeconds = 60;

    private static (InMemoryApplicationDbContext DbContext, FakeExternalMatchDataService Provider, LiveScoreService Service) MakeService()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var provider = new FakeExternalMatchDataService();
        var options = Options.Create(new FootballDataApiOptions { LiveScorePollSeconds = PollSeconds });
        var service = new LiveScoreService(dbContext, provider, options, NullLogger<LiveScoreService>.Instance);

        return (dbContext, provider, service);
    }

    /// <summary>
    /// Adds a competition and one match to the context, defaulting to a match that kicked off an
    /// hour ago - i.e. one the poller should be interested in.
    /// </summary>
    private static DomainEntities.Match GivenMatch(
        InMemoryApplicationDbContext dbContext,
        int externalMatchId = 101,
        DateTime? kickoff = null,
        bool matchPlayed = false,
        string? competitionCode = CompetitionCode)
    {
        var competition = new DomainEntities.Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = "Test Cup",
            ExternalApiCompetitionCode = competitionCode,
        };
        dbContext.Competition.Add(competition);

        var match = new DomainEntities.Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            Competition = competition,
            MatchDateTime = kickoff ?? UkClock.Now.AddHours(-1),
            MatchPlayed = matchPlayed,
            ExternalMatchID = externalMatchId,
        };
        dbContext.Match.Add(match);

        return match;
    }

    private static ExternalMatchScore Reported(int externalMatchId, int home, int away, string status = "IN_PLAY")
        => new() { ExternalMatchID = externalMatchId, HomeTeamGoals = home, AwayTeamGoals = away, Status = status };

    [Fact]
    public async Task RefreshAsync_StoresTheReportedScore_ForAMatchWeHaveNotHeardAboutBefore()
    {
        var (dbContext, provider, service) = MakeService();
        var match = GivenMatch(dbContext);
        await dbContext.SaveChangesAsync();

        provider.Scores = [Reported(match.ExternalMatchID!.Value, 1, 0)];

        var summary = await service.RefreshAsync();

        summary.MatchesInPlay.Should().Be(1);
        summary.ScoresChanged.Should().Be(1);

        var stored = dbContext.MatchLiveScore.Single();
        stored.HomeTeamGoals.Should().Be(1);
        stored.AwayTeamGoals.Should().Be(0);
        stored.Source.Should().Be(LiveScoreSource.Api);
        stored.LastPolledDateTime.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshAsync_TakesAHigherScore()
    {
        var (dbContext, provider, service) = MakeService();
        var match = GivenMatch(dbContext);
        GivenStoredScore(dbContext, match.MatchID, 1, 0);
        await dbContext.SaveChangesAsync();

        provider.Scores = [Reported(match.ExternalMatchID!.Value, 2, 1)];

        var summary = await service.RefreshAsync();

        summary.ScoresChanged.Should().Be(1);
        var stored = dbContext.MatchLiveScore.Single();
        (stored.HomeTeamGoals, stored.AwayTeamGoals).Should().Be((2, 1));
    }

    [Fact]
    public async Task RefreshAsync_TakesALowerScore_WhenAGoalIsDisallowed()
    {
        var (dbContext, provider, service) = MakeService();
        var match = GivenMatch(dbContext);
        var stored = GivenStoredScore(dbContext, match.MatchID, 0, 1);
        await dbContext.SaveChangesAsync();

        // A goal given on the pitch and then ruled out by VAR: the provider reported it, then took
        // it back. Refusing the decrease would leave a goalless match showing 0-1 until full time.
        provider.Scores = [Reported(match.ExternalMatchID!.Value, 0, 0)];

        var summary = await service.RefreshAsync();

        summary.ScoresChanged.Should().Be(1);
        (stored.HomeTeamGoals, stored.AwayTeamGoals).Should().Be((0, 0));
    }

    [Fact]
    public async Task RefreshAsync_TakesAReportWhereOnlyOneSideIsLower()
    {
        var (dbContext, provider, service) = MakeService();
        var match = GivenMatch(dbContext);
        var stored = GivenStoredScore(dbContext, match.MatchID, 2, 1);
        await dbContext.SaveChangesAsync();

        // One side up and the other down in the same poll - a goal at one end and a disallowed one
        // at the other. The report is taken whole, never half of it mixed with what we held.
        provider.Scores = [Reported(match.ExternalMatchID!.Value, 3, 0)];

        await service.RefreshAsync();

        (stored.HomeTeamGoals, stored.AwayTeamGoals).Should().Be((3, 0));
    }

    [Fact]
    public async Task RefreshAsync_HoldsAnAdminScore_WhileTheFeedCatchesUp()
    {
        var (dbContext, provider, service) = MakeService();
        var match = GivenMatch(dbContext);
        var stored = GivenStoredScore(
            dbContext, match.MatchID, 2, 1, source: LiveScoreSource.Admin, updatedMinutesAgo: 1);
        await dbContext.SaveChangesAsync();

        // The free tier's feed runs behind, so it can still be reporting the score as it was before
        // an admin entered the goal they just watched go in.
        provider.Scores = [Reported(match.ExternalMatchID!.Value, 1, 1)];

        var summary = await service.RefreshAsync();

        summary.ScoresChanged.Should().Be(0);
        (stored.HomeTeamGoals, stored.AwayTeamGoals).Should().Be((2, 1));
        stored.Source.Should().Be(LiveScoreSource.Admin, "an ignored report shouldn't claim authorship either");
        stored.LastPolledDateTime.Should().NotBeNull("we did hear from the provider, we just didn't believe it");
    }

    [Fact]
    public async Task RefreshAsync_HandsBackToTheProvider_OnceAnAdminScoreHasHadItsHold()
    {
        var (dbContext, provider, service) = MakeService();
        var match = GivenMatch(dbContext);
        var stored = GivenStoredScore(
            dbContext, match.MatchID, 2, 1, source: LiveScoreSource.Admin, updatedMinutesAgo: 6);
        await dbContext.SaveChangesAsync();

        // Well past the hold, so the feed has had its chance to catch up and drives again - without
        // this, one hand-entered goal would leave an admin entering every goal after it.
        provider.Scores = [Reported(match.ExternalMatchID!.Value, 1, 1)];

        var summary = await service.RefreshAsync();

        summary.ScoresChanged.Should().Be(1);
        (stored.HomeTeamGoals, stored.AwayTeamGoals).Should().Be((1, 1));
        stored.Source.Should().Be(LiveScoreSource.Api);
        stored.UpdatedByUserID.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_TakesTheFinalScore_OverAnAdminScoreStillInsideItsHold()
    {
        var (dbContext, provider, service) = MakeService();
        var match = GivenMatch(dbContext);
        var stored = GivenStoredScore(
            dbContext, match.MatchID, 2, 1, source: LiveScoreSource.Admin, updatedMinutesAgo: 1);
        await dbContext.SaveChangesAsync();

        provider.Scores = [Reported(match.ExternalMatchID!.Value, 1, 1, ExternalMatchScore.FinishedStatus)];

        var summary = await service.RefreshAsync();

        summary.ScoresChanged.Should().Be(1);
        (stored.HomeTeamGoals, stored.AwayTeamGoals).Should().Be((1, 1),
            "a settled full-time score outranks a hold meant only to cover a lagging feed");
    }

    [Fact]
    public async Task RefreshAsync_StopsPollingAMatchTheProviderHasCalledFinished()
    {
        var (dbContext, provider, service) = MakeService();
        var match = GivenMatch(dbContext);
        GivenStoredScore(dbContext, match.MatchID, 2, 1, status: ExternalMatchScore.FinishedStatus);
        await dbContext.SaveChangesAsync();

        var summary = await service.RefreshAsync();

        summary.MatchesInPlay.Should().Be(0);
        provider.ScoreRequests.Should().BeEmpty(
            "re-polling a finished match would overwrite any correction an admin made after full time");
    }

    [Theory]
    [InlineData(true, null, "a match with a confirmed result is settled")]
    [InlineData(false, null, "a match with no external id can't be looked up")]
    public async Task RefreshAsync_SkipsMatchesTheProviderCannotHelpWith(bool matchPlayed, int? externalMatchId, string because)
    {
        var (dbContext, provider, service) = MakeService();
        var match = GivenMatch(dbContext, matchPlayed: matchPlayed);
        match.ExternalMatchID = matchPlayed ? match.ExternalMatchID : externalMatchId;
        await dbContext.SaveChangesAsync();

        var summary = await service.RefreshAsync();

        summary.MatchesInPlay.Should().Be(0, because);
        provider.ScoreRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshAsync_SkipsAMatchWhoseCompetitionHasNoExternalCode()
    {
        var (dbContext, provider, service) = MakeService();
        GivenMatch(dbContext, competitionCode: null);
        await dbContext.SaveChangesAsync();

        var summary = await service.RefreshAsync();

        summary.MatchesInPlay.Should().Be(0);
        provider.ScoreRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshAsync_SkipsAMatchThatKickedOffLongerAgoThanAnyMatchLasts()
    {
        var (dbContext, provider, service) = MakeService();
        GivenMatch(dbContext, kickoff: UkClock.Now.AddHours(-9));
        await dbContext.SaveChangesAsync();

        var summary = await service.RefreshAsync();

        summary.MatchesInPlay.Should().Be(0, "a fixture nobody ever processed shouldn't be polled forever");
    }

    [Fact]
    public async Task RefreshAsync_StandsDown_WhenAnotherWorkerPolledInsideTheInterval()
    {
        var (dbContext, provider, service) = MakeService();
        var match = GivenMatch(dbContext);
        var stored = GivenStoredScore(dbContext, match.MatchID, 1, 0);
        stored.LastPolledDateTime = UkClock.Now.AddSeconds(-5);
        await dbContext.SaveChangesAsync();

        provider.Scores = [Reported(match.ExternalMatchID!.Value, 2, 0)];

        var summary = await service.RefreshAsync();

        summary.SkippedReason.Should().NotBeNull();
        provider.ScoreRequests.Should().BeEmpty();
        (stored.HomeTeamGoals, stored.AwayTeamGoals).Should().Be((1, 0));
    }

    [Fact]
    public async Task RefreshAsync_AsksTheProviderOncePerCompetition_NotOncePerMatch()
    {
        var (dbContext, provider, service) = MakeService();
        var competition = new DomainEntities.Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = "Test Cup",
            ExternalApiCompetitionCode = CompetitionCode,
        };
        dbContext.Competition.Add(competition);

        foreach (var externalId in new[] { 101, 102, 103 })
        {
            dbContext.Match.Add(new DomainEntities.Match
            {
                MatchID = Guid.NewGuid(),
                CompetitionID = competition.CompetitionID,
                Competition = competition,
                MatchDateTime = UkClock.Now.AddMinutes(-30),
                ExternalMatchID = externalId,
            });
        }

        await dbContext.SaveChangesAsync();
        provider.Scores = [Reported(101, 1, 0), Reported(102, 0, 0), Reported(103, 2, 2)];

        var summary = await service.RefreshAsync();

        summary.MatchesInPlay.Should().Be(3);
        summary.ScoresChanged.Should().Be(3);
        provider.ScoreRequests.Should().ContainSingle("calls are scarce - a whole day of a competition arrives in one");
    }

    [Fact]
    public async Task GetNextRefreshDelayAsync_UsesThePollInterval_WhileAMatchIsInPlay()
    {
        var (dbContext, _, service) = MakeService();
        GivenMatch(dbContext);
        await dbContext.SaveChangesAsync();

        var delay = await service.GetNextRefreshDelayAsync();

        delay.Should().Be(TimeSpan.FromSeconds(PollSeconds));
    }

    [Fact]
    public async Task GetNextRefreshDelayAsync_WaitsForTheNextKickOff_WhenNothingIsInPlay()
    {
        var (dbContext, _, service) = MakeService();
        GivenMatch(dbContext, kickoff: UkClock.Now.AddMinutes(20));
        await dbContext.SaveChangesAsync();

        var delay = await service.GetNextRefreshDelayAsync();

        delay.Should().BeCloseTo(TimeSpan.FromMinutes(20), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetNextRefreshDelayAsync_CapsTheWait_SoNewFixturesGetNoticedWithinTheHour()
    {
        var (dbContext, _, service) = MakeService();
        GivenMatch(dbContext, kickoff: UkClock.Now.AddDays(3));
        await dbContext.SaveChangesAsync();

        var delay = await service.GetNextRefreshDelayAsync();

        delay.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task GetNextRefreshDelayAsync_CapsTheWait_WhenThereAreNoFixturesAtAll()
    {
        var (_, _, service) = MakeService();

        var delay = await service.GetNextRefreshDelayAsync();

        delay.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task SaveAdminScoreAsync_MayLowerAScore()
    {
        var (dbContext, _, service) = MakeService();
        var match = GivenMatch(dbContext);
        GivenStoredScore(dbContext, match.MatchID, 2, 1);
        await dbContext.SaveChangesAsync();

        var userId = Guid.NewGuid();

        var result = await service.SaveAdminScoreAsync(match.MatchID, 1, 1, userId);

        result.IsSuccess.Should().BeTrue();
        var stored = dbContext.MatchLiveScore.Single();
        (stored.HomeTeamGoals, stored.AwayTeamGoals).Should().Be((1, 1));
        stored.Source.Should().Be(LiveScoreSource.Admin);
        stored.UpdatedByUserID.Should().Be(userId);
    }

    [Fact]
    public async Task SaveAdminScoreAsync_KeepsTheProvidersStatus_SoAFinishedMatchStaysRetired()
    {
        var (dbContext, _, service) = MakeService();
        var match = GivenMatch(dbContext);
        GivenStoredScore(dbContext, match.MatchID, 2, 1, status: ExternalMatchScore.FinishedStatus);
        await dbContext.SaveChangesAsync();

        await service.SaveAdminScoreAsync(match.MatchID, 3, 1, Guid.NewGuid());

        // Clearing the status would put the match back in the poll set, and the next pass would
        // overwrite this correction with the provider's final score.
        dbContext.MatchLiveScore.Single().Status.Should().Be(ExternalMatchScore.FinishedStatus);
    }

    [Fact]
    public async Task SaveAdminScoreAsync_RefusesAMatchThatAlreadyHasAConfirmedResult()
    {
        var (dbContext, _, service) = MakeService();
        var match = GivenMatch(dbContext, matchPlayed: true);
        await dbContext.SaveChangesAsync();

        var result = await service.SaveAdminScoreAsync(match.MatchID, 1, 0, Guid.NewGuid());

        result.IsFailed.Should().BeTrue("a scored result is the Process Results page's business, not this one's");
    }

    [Fact]
    public async Task SaveAdminScoreAsync_RefusesANegativeScore()
    {
        var (dbContext, _, service) = MakeService();
        var match = GivenMatch(dbContext);
        await dbContext.SaveChangesAsync();

        var result = await service.SaveAdminScoreAsync(match.MatchID, -1, 0, Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAdminScoreAsync_FailsForAMatchThatDoesNotExist()
    {
        var (_, _, service) = MakeService();

        var result = await service.SaveAdminScoreAsync(Guid.NewGuid(), 1, 0, Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
    }

    private static DomainEntities.MatchLiveScore GivenStoredScore(
        InMemoryApplicationDbContext dbContext,
        Guid matchId,
        int home,
        int away,
        string source = LiveScoreSource.Api,
        string? status = "IN_PLAY",
        int updatedMinutesAgo = 10)
    {
        var liveScore = new DomainEntities.MatchLiveScore
        {
            MatchID = matchId,
            HomeTeamGoals = home,
            AwayTeamGoals = away,
            Status = status,
            Source = source,
            // Ten minutes ago by default, which is outside the admin hold - a test that cares about
            // the hold says so, rather than every other test depending on where the line sits.
            UpdatedDateTime = UkClock.Now.AddMinutes(-updatedMinutesAgo),
        };

        dbContext.MatchLiveScore.Add(liveScore);

        return liveScore;
    }
}
