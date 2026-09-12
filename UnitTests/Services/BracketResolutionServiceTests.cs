using FluentAssertions;
using Moq;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class BracketResolutionServiceTests
{
    private static readonly Guid CompetitionId = Guid.NewGuid();

    private static DomainEntities.Team MakeTeam(string name) => new()
    {
        TeamID = Guid.NewGuid(),
        TeamName = name,
        ShortName = name,
    };

    /// A knockout tie with both sides still to be decided.
    private static DomainEntities.Match MakeTie(string description, int knockoutRound, int bracketSlot, string homeTBC, string awayTBC) => new()
    {
        MatchID = Guid.NewGuid(),
        CompetitionID = CompetitionId,
        MatchDateTime = DateTime.Now.AddDays(3),
        Description = description,
        Knockout = true,
        KnockoutRound = knockoutRound,
        BracketSlot = bracketSlot,
        HomeTeamTBC = homeTBC,
        AwayTeamTBC = awayTBC,
    };

    private static GroupStandingsModel MakeGroup(string groupName, int matchesRemaining, params DomainEntities.Team[] inOrder) => new()
    {
        GroupName = groupName,
        MatchesRemaining = matchesRemaining,
        Standings = [.. inOrder.Select((team, index) => new TeamStandingItem
        {
            Position = index + 1,
            TeamID = team.TeamID,
            TeamName = team.TeamName,
        })],
    };

    private static (InMemoryApplicationDbContext DbContext, BracketResolutionService Service) MakeService(
        IReadOnlyList<GroupStandingsModel> groups)
    {
        var dbContext = new InMemoryApplicationDbContext();
        var teamService = new Mock<ITeamService>();
        teamService
            .Setup(t => t.GetGroupStandingsAsync(CompetitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(groups);

        return (dbContext, new BracketResolutionService(dbContext, teamService.Object));
    }

    [Fact]
    public async Task GetAsync_GroupHasFinished_ProposesItsWinnerAndRunnerUp()
    {
        var england = MakeTeam("England");
        var senegal = MakeTeam("Senegal");
        var (dbContext, service) = MakeService([MakeGroup("Group A", matchesRemaining: 0, england, senegal)]);

        dbContext.Team.AddRange(england, senegal);
        dbContext.Match.Add(MakeTie("Round of 16 1", 16, 1, "Winner Group A", "Runner-up Group A"));
        await dbContext.SaveChangesAsync();

        var resolution = await service.GetAsync(CompetitionId);

        var slots = resolution.Rounds.Single().Slots;
        slots.Should().AllSatisfy(s => s.Status.Should().Be(BracketSlotStatus.Ready));
        slots[0].ProposedTeamID.Should().Be(england.TeamID);
        slots[1].ProposedTeamID.Should().Be(senegal.TeamID);
        resolution.ReadyCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_GroupStillPlaying_WaitsAndSaysHowMuchIsLeft()
    {
        // A group two matches from the end has a leader, not a winner. Proposing off it would be a
        // guess dressed up as a fact, and the admin would have no way to tell the difference.
        var england = MakeTeam("England");
        var (dbContext, service) = MakeService([MakeGroup("Group A", matchesRemaining: 2, england)]);

        dbContext.Team.Add(england);
        dbContext.Match.Add(MakeTie("Round of 16 1", 16, 1, "Winner Group A", "Winner Group A"));
        await dbContext.SaveChangesAsync();

        var resolution = await service.GetAsync(CompetitionId);

        var slot = resolution.Rounds.Single().Slots[0];
        slot.Status.Should().Be(BracketSlotStatus.Waiting);
        slot.ProposedTeamID.Should().BeNull();
        slot.Reason.Should().Be("Group A: 2 matches left.");
        resolution.ReadyCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_FeedingTieWasWon_ProposesTheWinner()
    {
        var england = MakeTeam("England");
        var senegal = MakeTeam("Senegal");
        var (dbContext, service) = MakeService([]);

        var lastSixteen = MakeTie("Round of 16 1", 16, 1, "Winner Group A", "Runner-up Group B");
        lastSixteen.HomeTeamID = england.TeamID;
        lastSixteen.AwayTeamID = senegal.TeamID;
        lastSixteen.MatchPlayed = true;
        lastSixteen.HomeTeamGoals = 3;
        lastSixteen.AwayTeamGoals = 0;

        dbContext.Team.AddRange(england, senegal);
        dbContext.Match.AddRange(lastSixteen, MakeTie("Quarter-final 1", 8, 1, "Winner R16 1", "Winner R16 2"));
        await dbContext.SaveChangesAsync();

        var resolution = await service.GetAsync(CompetitionId);

        var quarterFinal = resolution.Rounds.Single(r => r.KnockoutRound == 8);
        quarterFinal.Slots[0].Status.Should().Be(BracketSlotStatus.Ready);
        quarterFinal.Slots[0].ProposedTeamID.Should().Be(england.TeamID);
    }

    [Fact]
    public async Task GetAsync_FeedingTieFinishedLevel_AsksTheAdminWhoWentThrough()
    {
        // The ordinary way a knockout tie ends. Extra time and penalties aren't in this database, so
        // the score at ninety minutes cannot say who progressed and nothing here should pretend.
        var england = MakeTeam("England");
        var senegal = MakeTeam("Senegal");
        var (dbContext, service) = MakeService([]);

        var lastSixteen = MakeTie("Round of 16 1", 16, 1, "Winner Group A", "Runner-up Group B");
        lastSixteen.HomeTeamID = england.TeamID;
        lastSixteen.AwayTeamID = senegal.TeamID;
        lastSixteen.MatchPlayed = true;
        lastSixteen.HomeTeamGoals = 1;
        lastSixteen.AwayTeamGoals = 1;

        dbContext.Team.AddRange(england, senegal);
        dbContext.Match.AddRange(lastSixteen, MakeTie("Quarter-final 1", 8, 1, "Winner R16 1", "Winner R16 2"));
        await dbContext.SaveChangesAsync();

        var resolution = await service.GetAsync(CompetitionId);

        var slot = resolution.Rounds.Single(r => r.KnockoutRound == 8).Slots[0];
        slot.Status.Should().Be(BracketSlotStatus.Manual);
        slot.Reason.Should().Be("Round of 16 1 finished 1 - 1. Who went through?");
    }

    [Fact]
    public async Task GetAsync_PlaceholderItCannotRead_LeavesItToTheAdmin()
    {
        // The Euros' four best third-placed teams land here: which last-16 tie they go to depends on
        // which groups they came from, via a table UEFA publishes and this doesn't model.
        var (dbContext, service) = MakeService([]);

        dbContext.Match.Add(MakeTie("Round of 16 3", 16, 3, "3rd Group A/B/F", "Winner Group C"));
        await dbContext.SaveChangesAsync();

        var resolution = await service.GetAsync(CompetitionId);

        var slot = resolution.Rounds.Single().Slots[0];
        slot.Status.Should().Be(BracketSlotStatus.Manual);
        slot.Reason.Should().Contain("isn't a placeholder this can read");
    }

    [Fact]
    public async Task ApplyAsync_FillsEmptySlotsAndLeavesSettledOnesAlone()
    {
        var england = MakeTeam("England");
        var senegal = MakeTeam("Senegal");
        var (dbContext, service) = MakeService([]);

        var tie = MakeTie("Round of 16 1", 16, 1, "Winner Group A", "Runner-up Group B");
        tie.HomeTeamID = england.TeamID;

        dbContext.Team.AddRange(england, senegal);
        dbContext.Match.Add(tie);
        await dbContext.SaveChangesAsync();

        // Posting a screen that has gone stale: it still thinks the home slot is empty. An admin's
        // own correction has to survive that, so the home side is left exactly as it is.
        var result = await service.ApplyAsync(CompetitionId, [
            new BracketSlotAssignment { MatchID = tie.MatchID, IsHome = true, TeamID = senegal.TeamID },
            new BracketSlotAssignment { MatchID = tie.MatchID, IsHome = false, TeamID = senegal.TeamID },
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Value.SlotsFilled.Should().Be(1);

        var saved = dbContext.Match.Single(m => m.MatchID == tie.MatchID);
        saved.HomeTeamID.Should().Be(england.TeamID, "a slot that already held a team is not overwritten");
        saved.AwayTeamID.Should().Be(senegal.TeamID);
        saved.HomeTeamTBC.Should().Be("Winner Group A", "the placeholder is the record of where the team came from");
    }

    [Fact]
    public async Task ApplyAsync_MatchFromAnotherCompetition_IsRefused()
    {
        var england = MakeTeam("England");
        var (dbContext, service) = MakeService([]);

        var elsewhere = MakeTie("Round of 16 1", 16, 1, "Winner Group A", "Runner-up Group B");
        elsewhere.CompetitionID = Guid.NewGuid();

        dbContext.Team.Add(england);
        dbContext.Match.Add(elsewhere);
        await dbContext.SaveChangesAsync();

        var result = await service.ApplyAsync(CompetitionId, [
            new BracketSlotAssignment { MatchID = elsewhere.MatchID, IsHome = true, TeamID = england.TeamID },
        ]);

        result.IsFailed.Should().BeTrue();
    }
}
