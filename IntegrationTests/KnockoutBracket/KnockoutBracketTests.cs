using FluentAssertions;
using MapsterMapper;
using Predictathon.Application.Common;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;

namespace Predictathon.IntegrationTests.KnockoutBracket;

/// <summary>
/// Exercises MatchService.GetKnockoutBracketAsync against the real dbo.UserMatchPredictionListGet -
/// its @BracketOnly branch, which ignores the date window a week list would use and orders by round
/// and draw position instead. Not reachable from the unit tests, whose InMemory fake returns nothing
/// from a stored procedure.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class KnockoutBracketTests
{
    private readonly DatabaseFixture _fixture;

    public KnockoutBracketTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static MatchService MakeService(ApplicationDbContext dbContext)
    {
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateMatchModel, MatchModel>(dbContext, new Mapper());
        return new MatchService(dependencyAggregate, dbContext, dbContext, new LeagueDataCache());
    }

    [Fact]
    public async Task GetKnockoutBracketAsync_ReturnsTheRoundsInOrderWithThePlayOffHeldBack()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var world = await SeedBracketAsync(dbContext, wholeBracket: true);

        try
        {
            var bracket = await MakeService(dbContext).GetKnockoutBracketAsync(world.UserId, world.CompetitionId);

            bracket.Rounds.Select(r => r.KnockoutRound).Should().Equal(8, 4, 2);
            bracket.Rounds.Select(r => r.RoundName).Should().Equal("Quarter final", "Semi final", "Final");
            bracket.Rounds.Select(r => r.Matches.Count).Should().Equal(4, 2, 1);
            bracket.IsWellFormed.Should().BeTrue();

            // The play-off is a knockout match but no part of the tree, so it comes back on its own
            // rather than as a round between the semi-finals and the final.
            bracket.Rounds.Should().NotContain(r => r.KnockoutRound == KnockoutRounds.ThirdPlacePlayOffRound);
            bracket.ThirdPlacePlayOff.Should().NotBeNull();
            bracket.ThirdPlacePlayOff!.KnockoutRound.Should().Be(KnockoutRounds.ThirdPlacePlayOffRound);
        }
        finally
        {
            await CleanUpAsync(dbContext, world);
        }
    }

    [Fact]
    public async Task GetKnockoutBracketAsync_OrdersEachRoundByDrawPositionRatherThanKickOff()
    {
        await using var dbContext = _fixture.CreateDbContext();

        // The quarter-finals are seeded with kick-offs deliberately opposite to their draw order, so
        // only an ordering that actually uses BracketSlot comes out 1, 2, 3, 4.
        var world = await SeedBracketAsync(dbContext, wholeBracket: true);

        try
        {
            var bracket = await MakeService(dbContext).GetKnockoutBracketAsync(world.UserId, world.CompetitionId);

            var quarterFinals = bracket.Rounds.Single(r => r.KnockoutRound == 8);
            quarterFinals.Matches.Select(m => m.BracketSlot).Should().Equal(1, 2, 3, 4);
            quarterFinals.Matches.Select(m => m.MatchDateTime).Should().BeInDescendingOrder(
                "the fixtures were seeded in the opposite order to the draw, so this proves the slot decided it");
        }
        finally
        {
            await CleanUpAsync(dbContext, world);
        }
    }

    [Fact]
    public async Task GetKnockoutBracketAsync_CarriesTheCallersOwnPredictionAndIgnoresOtherPeoples()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var world = await SeedBracketAsync(dbContext, wholeBracket: true);

        try
        {
            var bracket = await MakeService(dbContext).GetKnockoutBracketAsync(world.UserId, world.CompetitionId);

            var predicted = bracket.Rounds.Single(r => r.KnockoutRound == 8).Matches.Single(m => m.BracketSlot == 1);
            predicted.HomeTeamGoals.Should().Be(3);
            predicted.AwayTeamGoals.Should().Be(0);
        }
        finally
        {
            await CleanUpAsync(dbContext, world);
        }
    }

    [Fact]
    public async Task GetKnockoutBracketAsync_LeavesOutGroupMatchesHoweverRecentTheyAre()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var world = await SeedBracketAsync(dbContext, wholeBracket: true);

        try
        {
            var bracket = await MakeService(dbContext).GetKnockoutBracketAsync(world.UserId, world.CompetitionId);

            var everyMatch = bracket.Rounds.SelectMany(r => r.Matches).ToList();
            everyMatch.Should().NotContain(m => m.MatchID == world.GroupMatchId);
            everyMatch.Should().OnlyContain(m => m.KnockoutRound.HasValue);
        }
        finally
        {
            await CleanUpAsync(dbContext, world);
        }
    }

    [Fact]
    public async Task GetKnockoutBracketAsync_ReportsAHalfNumberedBracketAsNotWellFormed()
    {
        await using var dbContext = _fixture.CreateDbContext();

        // A quarter-final round one match short of the four it needs - the shape an admin leaves
        // behind part-way through numbering a draw.
        var world = await SeedBracketAsync(dbContext, wholeBracket: false);

        try
        {
            var bracket = await MakeService(dbContext).GetKnockoutBracketAsync(world.UserId, world.CompetitionId);

            bracket.Rounds.Should().NotBeEmpty("the view still needs to know there is a bracket to offer");
            bracket.IsWellFormed.Should().BeFalse("a tree with a hole in it is shown as the plain match list instead");
        }
        finally
        {
            await CleanUpAsync(dbContext, world);
        }
    }

    /// <summary>
    /// Seeds a competition with a quarter-final-onwards bracket plus one group match, and a
    /// prediction of the caller's against the first quarter-final.
    /// </summary>
    /// <param name="dbContext">The context to seed through.</param>
    /// <param name="wholeBracket">
    /// False to leave the quarter-finals a match short, so the bracket is incomplete.
    /// </param>
    private static async Task<SeededBracket> SeedBracketAsync(ApplicationDbContext dbContext, bool wholeBracket)
    {
        var now = UkClock.Now;

        var competition = new Competition
        {
            CompetitionID = Guid.NewGuid(),
            CompetitionName = $"Integration Test {Guid.NewGuid():N}",
            StartDate = DateOnly.FromDateTime(now.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(now.AddDays(30)),
        };
        dbContext.Competition.Add(competition);

        var you = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"you-{Guid.NewGuid():N}" };
        var someoneElse = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"other-{Guid.NewGuid():N}" };
        dbContext.Users.AddRange(you, someoneElse);

        var home = new Team { TeamID = Guid.NewGuid(), TeamName = $"Home {Guid.NewGuid():N}", ShortName = "HOM" };
        var away = new Team { TeamID = Guid.NewGuid(), TeamName = $"Away {Guid.NewGuid():N}", ShortName = "AWY" };
        dbContext.Team.AddRange(home, away);

        var matches = new List<Match>();

        // Kick-offs run backwards through the draw, so an ordering that fell back to the calendar
        // would come out 4, 3, 2, 1 and the draw-order test would catch it.
        var quarterFinalCount = wholeBracket ? 4 : 3;
        for (var slot = 1; slot <= quarterFinalCount; slot++)
        {
            matches.Add(MakeMatch(competition, home, away, now.AddDays(30 - slot), knockoutRound: 8, bracketSlot: slot));
        }

        matches.Add(MakeMatch(competition, home, away, now.AddDays(34), knockoutRound: 4, bracketSlot: 1));
        matches.Add(MakeMatch(competition, home, away, now.AddDays(34), knockoutRound: 4, bracketSlot: 2));
        matches.Add(MakeMatch(competition, home, away, now.AddDays(37), knockoutRound: KnockoutRounds.ThirdPlacePlayOffRound, bracketSlot: 1));
        matches.Add(MakeMatch(competition, home, away, now.AddDays(38), knockoutRound: 2, bracketSlot: 1));

        var groupMatch = MakeMatch(competition, home, away, now.AddDays(-1), knockoutRound: null, bracketSlot: null);
        matches.Add(groupMatch);

        dbContext.Match.AddRange(matches);

        var firstQuarterFinal = matches[0];
        dbContext.Prediction.AddRange(
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = firstQuarterFinal.MatchID, UserID = you.Id, HomeTeamGoals = 3, AwayTeamGoals = 0 },
            new Prediction { PredictionID = Guid.NewGuid(), MatchID = firstQuarterFinal.MatchID, UserID = someoneElse.Id, HomeTeamGoals = 1, AwayTeamGoals = 1 });

        await dbContext.SaveChangesAsync();

        return new SeededBracket(
            competition.CompetitionID,
            you.Id,
            [you.Id, someoneElse.Id],
            [.. matches.Select(m => m.MatchID)],
            groupMatch.MatchID,
            [home.TeamID, away.TeamID]);
    }

    private static Match MakeMatch(Competition competition, Team home, Team away, DateTime kickoff, int? knockoutRound, int? bracketSlot)
        => new()
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competition.CompetitionID,
            MatchDateTime = kickoff,
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            Knockout = knockoutRound.HasValue,
            KnockoutRound = knockoutRound,
            BracketSlot = bracketSlot,
        };

    private static async Task CleanUpAsync(ApplicationDbContext dbContext, SeededBracket world)
    {
        dbContext.Prediction.RemoveRange(dbContext.Prediction.Where(p => world.MatchIds.Contains(p.MatchID)));
        dbContext.Match.RemoveRange(dbContext.Match.Where(m => world.MatchIds.Contains(m.MatchID)));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => world.UserIds.Contains(u.Id)));
        dbContext.Team.RemoveRange(dbContext.Team.Where(t => world.TeamIds.Contains(t.TeamID)));
        dbContext.Competition.RemoveRange(dbContext.Competition.Where(c => c.CompetitionID == world.CompetitionId));
        await dbContext.SaveChangesAsync();
    }

    private sealed record SeededBracket(
        Guid CompetitionId,
        Guid UserId,
        IReadOnlyList<Guid> UserIds,
        IReadOnlyList<Guid> MatchIds,
        Guid GroupMatchId,
        IReadOnlyList<Guid> TeamIds);
}
