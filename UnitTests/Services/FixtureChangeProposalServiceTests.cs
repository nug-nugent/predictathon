using FluentAssertions;
using Predictathon.Application.Common;
using Predictathon.Application.Constants;
using Predictathon.Application.Errors;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class FixtureChangeProposalServiceTests
{
    private static DomainEntities.Competition MakeCompetition() => new()
    {
        CompetitionID = Guid.NewGuid(),
        CompetitionName = "Premier League 2026/27",
        StartDate = new DateOnly(2026, 8, 15),
        EndDate = new DateOnly(2027, 5, 24),
        ExternalApiCompetitionCode = "PL",
    };

    private static DomainEntities.Match MakeMatch(Guid competitionId, int externalMatchId, DateTime kickoffLocal) => new()
    {
        MatchID = Guid.NewGuid(),
        CompetitionID = competitionId,
        MatchDateTime = kickoffLocal,
        ExternalMatchID = externalMatchId,
    };

    private static ExternalFixture MakeFixture(int externalMatchId, DateTime kickoffUtc) => new()
    {
        ExternalMatchID = externalMatchId,
        KickoffUtc = kickoffUtc,
        HomeTeamExternalCode = "57",
        AwayTeamExternalCode = "61",
        HomeTeamName = "Arsenal FC",
        AwayTeamName = "Chelsea FC",
    };

    private static (InMemoryApplicationDbContext DbContext, FakeExternalMatchDataService ExternalMatchDataService, FixtureChangeProposalService Service) MakeService()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var externalMatchDataService = new FakeExternalMatchDataService();
        var service = new FixtureChangeProposalService(dbContext, externalMatchDataService);
        return (dbContext, externalMatchDataService, service);
    }

    [Fact]
    public async Task DetectChangesAsync_KickoffChanged_CreatesPendingProposal()
    {
        var (dbContext, externalMatchDataService, service) = MakeService();
        var competition = MakeCompetition();
        var originalKickoffUtc = new DateTime(2026, 8, 15, 14, 0, 0, DateTimeKind.Utc);
        var match = MakeMatch(competition.CompetitionID, externalMatchId: 100, UkClock.ToUkLocal(originalKickoffUtc));
        dbContext.Competition.Add(competition);
        dbContext.Match.Add(match);
        await dbContext.SaveChangesAsync();

        var newKickoffUtc = new DateTime(2026, 8, 16, 18, 0, 0, DateTimeKind.Utc);
        externalMatchDataService.Fixtures = [MakeFixture(100, newKickoffUtc)];

        var result = await service.DetectChangesAsync();

        result.IsSuccess.Should().BeTrue();
        var proposal = dbContext.FixtureChangeProposal.Should().ContainSingle().Subject;
        proposal.MatchID.Should().Be(match.MatchID);
        proposal.Status.Should().Be(FixtureChangeProposalStatuses.Pending);
        proposal.PreviousMatchDateTime.Should().Be(match.MatchDateTime);
        proposal.ProposedMatchDateTime.Should().Be(UkClock.ToUkLocal(newKickoffUtc));
    }

    [Fact]
    public async Task DetectChangesAsync_KickoffUnchanged_DoesNotCreateProposal()
    {
        var (dbContext, externalMatchDataService, service) = MakeService();
        var competition = MakeCompetition();
        var kickoffUtc = new DateTime(2026, 8, 15, 14, 0, 0, DateTimeKind.Utc);
        var match = MakeMatch(competition.CompetitionID, externalMatchId: 100, UkClock.ToUkLocal(kickoffUtc));
        dbContext.Competition.Add(competition);
        dbContext.Match.Add(match);
        await dbContext.SaveChangesAsync();

        externalMatchDataService.Fixtures = [MakeFixture(100, kickoffUtc)];

        await service.DetectChangesAsync();

        dbContext.FixtureChangeProposal.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectChangesAsync_FixtureMovesAgain_RefreshesExistingProposalRatherThanDuplicating()
    {
        var (dbContext, externalMatchDataService, service) = MakeService();
        var competition = MakeCompetition();
        var originalKickoffUtc = new DateTime(2026, 8, 15, 14, 0, 0, DateTimeKind.Utc);
        var match = MakeMatch(competition.CompetitionID, externalMatchId: 100, UkClock.ToUkLocal(originalKickoffUtc));
        dbContext.Competition.Add(competition);
        dbContext.Match.Add(match);
        dbContext.FixtureChangeProposal.Add(new DomainEntities.FixtureChangeProposal
        {
            MatchID = match.MatchID,
            PreviousMatchDateTime = match.MatchDateTime,
            ProposedMatchDateTime = UkClock.ToUkLocal(new DateTime(2026, 8, 16, 18, 0, 0, DateTimeKind.Utc)),
            Status = FixtureChangeProposalStatuses.Pending,
        });
        await dbContext.SaveChangesAsync();

        var thirdKickoffUtc = new DateTime(2026, 8, 17, 12, 30, 0, DateTimeKind.Utc);
        externalMatchDataService.Fixtures = [MakeFixture(100, thirdKickoffUtc)];

        await service.DetectChangesAsync();

        var proposal = dbContext.FixtureChangeProposal.Should().ContainSingle().Subject;
        proposal.Status.Should().Be(FixtureChangeProposalStatuses.Pending);
        proposal.PreviousMatchDateTime.Should().Be(match.MatchDateTime);
        proposal.ProposedMatchDateTime.Should().Be(UkClock.ToUkLocal(thirdKickoffUtc));
    }

    [Fact]
    public async Task DetectChangesAsync_ExternalTimeRevertsToStored_DismissesPendingProposal()
    {
        var (dbContext, externalMatchDataService, service) = MakeService();
        var competition = MakeCompetition();
        var originalKickoffUtc = new DateTime(2026, 8, 15, 14, 0, 0, DateTimeKind.Utc);
        var match = MakeMatch(competition.CompetitionID, externalMatchId: 100, UkClock.ToUkLocal(originalKickoffUtc));
        dbContext.Competition.Add(competition);
        dbContext.Match.Add(match);
        dbContext.FixtureChangeProposal.Add(new DomainEntities.FixtureChangeProposal
        {
            MatchID = match.MatchID,
            PreviousMatchDateTime = match.MatchDateTime,
            ProposedMatchDateTime = UkClock.ToUkLocal(new DateTime(2026, 8, 16, 18, 0, 0, DateTimeKind.Utc)),
            Status = FixtureChangeProposalStatuses.Pending,
        });
        await dbContext.SaveChangesAsync();

        // The broadcaster reverts the fixture back to its originally-stored kickoff.
        externalMatchDataService.Fixtures = [MakeFixture(100, originalKickoffUtc)];

        await service.DetectChangesAsync();

        var proposal = dbContext.FixtureChangeProposal.Should().ContainSingle().Subject;
        proposal.Status.Should().Be(FixtureChangeProposalStatuses.Dismissed);
        proposal.ResolvedAtUtc.Should().NotBeNull();
        match.MatchDateTime.Should().Be(UkClock.ToUkLocal(originalKickoffUtc));
    }

    [Fact]
    public async Task ConfirmAsync_PendingProposal_AppliesProposedTimeAndMarksConfirmed()
    {
        var (dbContext, _, service) = MakeService();
        var competition = MakeCompetition();
        var match = MakeMatch(competition.CompetitionID, externalMatchId: 100, new DateTime(2026, 8, 15, 15, 0, 0));
        var proposedTime = new DateTime(2026, 8, 16, 19, 0, 0);
        dbContext.Competition.Add(competition);
        dbContext.Match.Add(match);
        var proposal = new DomainEntities.FixtureChangeProposal
        {
            MatchID = match.MatchID,
            PreviousMatchDateTime = match.MatchDateTime,
            ProposedMatchDateTime = proposedTime,
            Status = FixtureChangeProposalStatuses.Pending,
        };
        dbContext.FixtureChangeProposal.Add(proposal);
        await dbContext.SaveChangesAsync();

        var result = await service.ConfirmAsync(proposal.FixtureChangeProposalID);

        result.IsSuccess.Should().BeTrue();
        match.MatchDateTime.Should().Be(proposedTime);
        proposal.Status.Should().Be(FixtureChangeProposalStatuses.Confirmed);
        proposal.ResolvedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmAsync_UnknownProposal_ReturnsNotFound()
    {
        var (_, _, service) = MakeService();

        var result = await service.ConfirmAsync(12345);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task DismissAsync_PendingProposal_MarksDismissedWithoutChangingMatch()
    {
        var (dbContext, _, service) = MakeService();
        var competition = MakeCompetition();
        var originalTime = new DateTime(2026, 8, 15, 15, 0, 0);
        var match = MakeMatch(competition.CompetitionID, externalMatchId: 100, originalTime);
        dbContext.Competition.Add(competition);
        dbContext.Match.Add(match);
        var proposal = new DomainEntities.FixtureChangeProposal
        {
            MatchID = match.MatchID,
            PreviousMatchDateTime = originalTime,
            ProposedMatchDateTime = new DateTime(2026, 8, 16, 19, 0, 0),
            Status = FixtureChangeProposalStatuses.Pending,
        };
        dbContext.FixtureChangeProposal.Add(proposal);
        await dbContext.SaveChangesAsync();

        var result = await service.DismissAsync(proposal.FixtureChangeProposalID);

        result.IsSuccess.Should().BeTrue();
        proposal.Status.Should().Be(FixtureChangeProposalStatuses.Dismissed);
        match.MatchDateTime.Should().Be(originalTime);
    }
}
