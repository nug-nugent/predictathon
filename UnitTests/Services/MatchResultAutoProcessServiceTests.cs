using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Predictathon.Application.Common;
using Predictathon.Application.Constants;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.Application.Options;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

/// <summary>
/// Covers which matches get their result confirmed without an admin: the provider having called the
/// match finished is the trigger, and everything else - a match still in play, one nobody has a
/// score for, one already processed, one too soon after kick-off - is left alone.
/// </summary>
public class MatchResultAutoProcessServiceTests
{
    private readonly InMemoryApplicationDbContext _dbContext = new();
    private readonly Mock<IMatchService> _matchService = new();

    private MatchResultAutoProcessService MakeService(bool autoProcess = true)
    {
        var options = Options.Create(new FootballDataApiOptions { AutoProcessFinishedResults = autoProcess });

        return new MatchResultAutoProcessService(
            _dbContext,
            _matchService.Object,
            options,
            NullLogger<MatchResultAutoProcessService>.Instance);
    }

    /// <summary>
    /// Makes <see cref="IMatchService.SaveResultAsync"/> succeed for any match, which is the normal
    /// case - the eligibility rule it enforces is pre-filtered before it's ever called.
    /// </summary>
    private void GivenResultsSaveSuccessfully()
    {
        _matchService
            .Setup(m => m.SaveResultAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new MatchModel()));
    }

    /// <summary>
    /// Adds a match to the context, defaulting to one that kicked off two hours ago - i.e. one whose
    /// result is eligible to be confirmed - together with the live score given for it.
    /// </summary>
    /// <param name="homeTeamGoals">The stored live score's home goals, or null for no stored score at all.</param>
    /// <param name="awayTeamGoals">The stored live score's away goals.</param>
    /// <param name="status">The provider's last reported status for the match.</param>
    /// <param name="source">Whether the stored score came from the provider or an admin.</param>
    /// <param name="kickoff">Kick-off time, defaulting to two hours ago.</param>
    /// <param name="matchPlayed">Whether the match already has a confirmed result.</param>
    private DomainEntities.Match GivenMatch(
        int? homeTeamGoals = 2,
        int awayTeamGoals = 1,
        string? status = ExternalMatchScore.FinishedStatus,
        string source = LiveScoreSource.Api,
        DateTime? kickoff = null,
        bool matchPlayed = false)
    {
        var match = new DomainEntities.Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = Guid.NewGuid(),
            MatchDateTime = kickoff ?? UkClock.Now.AddHours(-2),
            MatchPlayed = matchPlayed,
        };
        _dbContext.Match.Add(match);

        if (homeTeamGoals is not null)
        {
            var liveScore = new DomainEntities.MatchLiveScore
            {
                MatchID = match.MatchID,
                HomeTeamGoals = homeTeamGoals.Value,
                AwayTeamGoals = awayTeamGoals,
                Status = status,
                Source = source,
                UpdatedDateTime = UkClock.Now,
            };

            _dbContext.MatchLiveScore.Add(liveScore);
            match.MatchLiveScore = liveScore;
        }

        return match;
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_ConfirmsTheResult_FromAFinishedLiveScore()
    {
        GivenResultsSaveSuccessfully();
        var match = GivenMatch(homeTeamGoals: 3, awayTeamGoals: 0);
        await _dbContext.SaveChangesAsync();

        var summary = await MakeService().ProcessFinishedMatchesAsync();

        summary.ResultsProcessed.Should().Be(1);
        summary.ResultsFailed.Should().Be(0);
        _matchService.Verify(m => m.SaveResultAsync(match.MatchID, 3, 0, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_MatchesTheFinishedStatus_WithoutMindingItsCasing()
    {
        GivenResultsSaveSuccessfully();
        var match = GivenMatch(status: "finished");
        await _dbContext.SaveChangesAsync();

        var summary = await MakeService().ProcessFinishedMatchesAsync();

        summary.ResultsProcessed.Should().Be(1);
        _matchService.Verify(m => m.SaveResultAsync(match.MatchID, 2, 1, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_ConfirmsAnAdminsCorrection_WhenItSitsOnTopOfTheProvidersFinalScore()
    {
        // An admin correcting a final score leaves the provider's FINISHED status in place, so the
        // match is still auto-processed - with their number, which is the point of the correction.
        GivenResultsSaveSuccessfully();
        var match = GivenMatch(homeTeamGoals: 1, awayTeamGoals: 1, source: LiveScoreSource.Admin);
        await _dbContext.SaveChangesAsync();

        var summary = await MakeService().ProcessFinishedMatchesAsync();

        summary.ResultsProcessed.Should().Be(1);
        _matchService.Verify(m => m.SaveResultAsync(match.MatchID, 1, 1, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_LeavesAMatchAlone_WhileItIsStillInPlay()
    {
        GivenResultsSaveSuccessfully();
        GivenMatch(status: "IN_PLAY");
        await _dbContext.SaveChangesAsync();

        var summary = await MakeService().ProcessFinishedMatchesAsync();

        summary.ResultsProcessed.Should().Be(0);
        _matchService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_LeavesAMatchAlone_WhenNobodyHasAScoreForItAtAll()
    {
        GivenResultsSaveSuccessfully();
        GivenMatch(homeTeamGoals: null);
        await _dbContext.SaveChangesAsync();

        var summary = await MakeService().ProcessFinishedMatchesAsync();

        summary.ResultsProcessed.Should().Be(0);
        _matchService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_LeavesAMatchAlone_WhenItsResultIsAlreadyConfirmed()
    {
        // A processed match's live score row stays behind so the Live page can still show the
        // full-time scoreline; that mustn't be mistaken for a result waiting to be confirmed.
        GivenResultsSaveSuccessfully();
        GivenMatch(matchPlayed: true);
        await _dbContext.SaveChangesAsync();

        var summary = await MakeService().ProcessFinishedMatchesAsync();

        summary.ResultsProcessed.Should().Be(0);
        _matchService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_LeavesAMatchAlone_WhenItIsTooSoonAfterKickOff()
    {
        // A feed calling a match finished twenty minutes in has got something wrong - an abandonment,
        // say. Skipped rather than counted as a failure: there's nothing here for anyone to fix.
        GivenResultsSaveSuccessfully();
        GivenMatch(kickoff: UkClock.Now.AddMinutes(-20));
        await _dbContext.SaveChangesAsync();

        var summary = await MakeService().ProcessFinishedMatchesAsync();

        summary.ResultsProcessed.Should().Be(0);
        summary.ResultsFailed.Should().Be(0);
        _matchService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_CountsAFailedSave_AndCarriesOnWithTheRest()
    {
        var failing = GivenMatch(homeTeamGoals: 1, awayTeamGoals: 0);
        var succeeding = GivenMatch(homeTeamGoals: 2, awayTeamGoals: 2);
        await _dbContext.SaveChangesAsync();

        _matchService
            .Setup(m => m.SaveResultAsync(failing.MatchID, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<MatchModel>(new ConflictError("Nope.")));
        _matchService
            .Setup(m => m.SaveResultAsync(succeeding.MatchID, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new MatchModel()));

        var summary = await MakeService().ProcessFinishedMatchesAsync();

        summary.ResultsProcessed.Should().Be(1);
        summary.ResultsFailed.Should().Be(1);
        _matchService.Verify(m => m.SaveResultAsync(succeeding.MatchID, 2, 2, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessFinishedMatchesAsync_DoesNothing_WhenAutoProcessingIsSwitchedOff()
    {
        GivenResultsSaveSuccessfully();
        GivenMatch();
        await _dbContext.SaveChangesAsync();

        var summary = await MakeService(autoProcess: false).ProcessFinishedMatchesAsync();

        summary.ResultsProcessed.Should().Be(0);
        summary.SkippedReason.Should().NotBeNull();
        _matchService.VerifyNoOtherCalls();
    }
}
