using FluentAssertions;
using MapsterMapper;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

/// <summary>
/// Covers the admin helper that numbers a competition's bracket slots from its kick-off times.
///
/// What it can't do is the point of most of these: kick-off order is the schedule, and a
/// competition's schedule is not its draw, so the helper only ever produces a starting point an
/// admin then corrects. These tests pin down the parts that must be right for that to be a useful
/// starting point rather than a mess - each round numbered from one, nothing outside the bracket
/// touched, and the same fixtures always numbered the same way.
/// </summary>
public class MatchServiceBracketNumberingTests
{
    private static (InMemoryApplicationDbContext DbContext, MatchService Service) MakeService()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateMatchModel, MatchModel>(dbContext, new Mapper());
        var service = new MatchService(dependencyAggregate, dbContext, dbContext, new LeagueDataCache());
        return (dbContext, service);
    }

    /// <summary>
    /// A knockout match in a competition, kicking off at the given time.
    /// </summary>
    /// <param name="competitionId">The competition the match belongs to.</param>
    /// <param name="kickoff">When it kicks off.</param>
    /// <param name="knockoutRound">Its bracket round.</param>
    /// <param name="bracketSlot">Any slot it already carries.</param>
    private static DomainEntities.Match MakeBracketMatch(Guid competitionId, DateTime kickoff, int knockoutRound, int? bracketSlot = null)
        => new()
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competitionId,
            MatchDateTime = kickoff,
            Knockout = true,
            KnockoutRound = knockoutRound,
            BracketSlot = bracketSlot,
        };

    [Fact]
    public async Task NumberBracketByKickOffAsync_NumbersEachRoundFromOneInKickOffOrder()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var kickoff = new DateTime(2026, 7, 12, 15, 0, 0);

        // Added out of order, so only an ordering that actually sorts comes out 1, 2, 3, 4.
        var thirdQuarterFinal = MakeBracketMatch(competitionId, kickoff.AddDays(2), knockoutRound: 8);
        var firstQuarterFinal = MakeBracketMatch(competitionId, kickoff, knockoutRound: 8);
        var secondSemiFinal = MakeBracketMatch(competitionId, kickoff.AddDays(6).AddHours(4), knockoutRound: 4);
        var firstSemiFinal = MakeBracketMatch(competitionId, kickoff.AddDays(6), knockoutRound: 4);
        var secondQuarterFinal = MakeBracketMatch(competitionId, kickoff.AddHours(4), knockoutRound: 8);
        var fourthQuarterFinal = MakeBracketMatch(competitionId, kickoff.AddDays(2).AddHours(4), knockoutRound: 8);
        dbContext.Match.AddRange(thirdQuarterFinal, firstQuarterFinal, secondSemiFinal, firstSemiFinal, secondQuarterFinal, fourthQuarterFinal);
        await dbContext.SaveChangesAsync();

        var summary = await service.NumberBracketByKickOffAsync(competitionId);

        summary.MatchesNumbered.Should().Be(6);
        summary.RoundsNumbered.Should().Be(2, "the quarter-finals and the semi-finals");

        // Each round is numbered from 1 in its own right, not continuously across the bracket.
        firstQuarterFinal.BracketSlot.Should().Be(1);
        secondQuarterFinal.BracketSlot.Should().Be(2);
        thirdQuarterFinal.BracketSlot.Should().Be(3);
        fourthQuarterFinal.BracketSlot.Should().Be(4);
        firstSemiFinal.BracketSlot.Should().Be(1);
        secondSemiFinal.BracketSlot.Should().Be(2);
    }

    [Fact]
    public async Task NumberBracketByKickOffAsync_ReplacesSlotsAlreadySet()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var kickoff = new DateTime(2026, 7, 12, 15, 0, 0);

        var earlier = MakeBracketMatch(competitionId, kickoff, knockoutRound: 4, bracketSlot: 2);
        var later = MakeBracketMatch(competitionId, kickoff.AddHours(4), knockoutRound: 4, bracketSlot: 1);
        dbContext.Match.AddRange(earlier, later);
        await dbContext.SaveChangesAsync();

        await service.NumberBracketByKickOffAsync(competitionId);

        earlier.BracketSlot.Should().Be(1);
        later.BracketSlot.Should().Be(2);
    }

    [Fact]
    public async Task NumberBracketByKickOffAsync_LeavesMatchesWithNoRoundAlone()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var kickoff = new DateTime(2026, 7, 12, 15, 0, 0);

        // Which round a match is in is the part no schedule can imply, so a match without one is no
        // part of the bracket and is left exactly as it was.
        var groupMatch = new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = kickoff.AddDays(-5) };
        var quarterFinal = MakeBracketMatch(competitionId, kickoff, knockoutRound: 8);
        dbContext.Match.AddRange(groupMatch, quarterFinal);
        await dbContext.SaveChangesAsync();

        var summary = await service.NumberBracketByKickOffAsync(competitionId);

        summary.MatchesNumbered.Should().Be(1);
        groupMatch.BracketSlot.Should().BeNull();
        quarterFinal.BracketSlot.Should().Be(1);
    }

    [Fact]
    public async Task NumberBracketByKickOffAsync_LeavesOtherCompetitionsAlone()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var otherCompetitionId = Guid.NewGuid();
        var kickoff = new DateTime(2026, 7, 12, 15, 0, 0);

        var ours = MakeBracketMatch(competitionId, kickoff, knockoutRound: 2);
        var theirs = MakeBracketMatch(otherCompetitionId, kickoff, knockoutRound: 2);
        dbContext.Match.AddRange(ours, theirs);
        await dbContext.SaveChangesAsync();

        await service.NumberBracketByKickOffAsync(competitionId);

        ours.BracketSlot.Should().Be(1);
        theirs.BracketSlot.Should().BeNull();
    }

    [Fact]
    public async Task NumberBracketByKickOffAsync_IsStableWhenTwoTiesKickOffTogether()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var kickoff = new DateTime(2026, 7, 12, 15, 0, 0);

        // Simultaneous kick-offs are normal in a knockout round. Running the helper twice has to
        // leave the same numbering, or a second press would reshuffle the slots around whichever
        // ones an admin had just corrected.
        var together = new[]
        {
            MakeBracketMatch(competitionId, kickoff, knockoutRound: 8),
            MakeBracketMatch(competitionId, kickoff, knockoutRound: 8),
            MakeBracketMatch(competitionId, kickoff, knockoutRound: 8),
            MakeBracketMatch(competitionId, kickoff, knockoutRound: 8),
        };
        dbContext.Match.AddRange(together);
        await dbContext.SaveChangesAsync();

        await service.NumberBracketByKickOffAsync(competitionId);
        var firstPass = together.Select(m => m.BracketSlot).ToList();

        await service.NumberBracketByKickOffAsync(competitionId);

        together.Select(m => m.BracketSlot).Should().Equal(firstPass);
        firstPass.Should().BeEquivalentTo(new int?[] { 1, 2, 3, 4 });
    }
}
