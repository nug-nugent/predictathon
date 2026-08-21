using FluentAssertions;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class TeamServiceTests
{
    private static (InMemoryApplicationDbContext DbContext, TeamService Service) MakeService()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var service = new TeamService(dbContext);
        return (dbContext, service);
    }

    private static DomainEntities.Team MakeTeam(string name, string shortName)
        => new() { TeamID = Guid.NewGuid(), TeamName = name, ShortName = shortName };

    /// <summary>
    /// Registers teams for a competition, so they appear in its league table whether or not they've
    /// played yet.
    /// </summary>
    private static void Register(InMemoryApplicationDbContext dbContext, Guid competitionId, params DomainEntities.Team[] teams)
    {
        dbContext.Team.AddRange(teams);
        dbContext.TeamCompetition.AddRange(teams.Select(t => new DomainEntities.TeamCompetition
        {
            TeamCompetitionID = Guid.NewGuid(),
            CompetitionID = competitionId,
            TeamID = t.TeamID,
        }));
    }

    private static DomainEntities.Match MakePlayedMatch(Guid competitionId, DomainEntities.Team home, DomainEntities.Team away, int homeGoals, int awayGoals)
        => new()
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competitionId,
            MatchDateTime = new DateTime(2026, 8, 15, 15, 0, 0),
            HomeTeamID = home.TeamID,
            AwayTeamID = away.TeamID,
            MatchPlayed = true,
            HomeTeamGoals = homeGoals,
            AwayTeamGoals = awayGoals,
        };

    [Fact]
    public async Task GetTeamDetailAsync_ReturnsNull_WhenTeamDoesNotExist()
    {
        var (_, service) = MakeService();

        var detail = await service.GetTeamDetailAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        detail.Should().BeNull();
    }

    [Fact]
    public async Task GetTeamDetailAsync_ReturnsOnlyUnplayedFixtures_SoonestFirst()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var arsenal = MakeTeam("Arsenal", "ARS");
        var chelsea = MakeTeam("Chelsea", "CHE");
        var everton = MakeTeam("Everton", "EVE");
        Register(dbContext, competitionId, arsenal, chelsea, everton);

        var kickoff = new DateTime(2026, 8, 22, 15, 0, 0);
        dbContext.Match.AddRange(
            // Played - belongs in results, not fixtures.
            MakePlayedMatch(competitionId, arsenal, chelsea, 2, 1),
            new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = kickoff.AddDays(7), HomeTeamID = everton.TeamID, AwayTeamID = arsenal.TeamID },
            new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = kickoff, HomeTeamID = arsenal.TeamID, AwayTeamID = everton.TeamID },
            // Another team's fixture.
            new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = kickoff, HomeTeamID = chelsea.TeamID, AwayTeamID = everton.TeamID });
        await dbContext.SaveChangesAsync();

        var detail = await service.GetTeamDetailAsync(competitionId, arsenal.TeamID, Guid.NewGuid());

        detail!.Fixtures.Select(f => f.MatchDateTime).Should().Equal(kickoff, kickoff.AddDays(7));
        detail.Fixtures[0].HomeTeamShortName.Should().Be("ARS");
        detail.Fixtures[0].AwayTeam.Should().Be("Everton");
        detail.Fixtures[1].HomeTeamShortName.Should().Be("EVE");
    }

    [Fact]
    public async Task GetTeamDetailAsync_FixtureAgainstUndecidedOpponent_UsesTbcPlaceholder()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var england = MakeTeam("England", "ENG");
        Register(dbContext, competitionId, england);

        dbContext.Match.Add(new DomainEntities.Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competitionId,
            MatchDateTime = new DateTime(2026, 7, 5, 20, 0, 0),
            HomeTeamID = england.TeamID,
            AwayTeamID = null,
            AwayTeamTBC = "Winner of QF2",
            Knockout = true,
        });
        await dbContext.SaveChangesAsync();

        var detail = await service.GetTeamDetailAsync(competitionId, england.TeamID, Guid.NewGuid());

        detail!.Fixtures.Should().ContainSingle();
        detail.Fixtures[0].AwayTeam.Should().Be("Winner of QF2");
        detail.Fixtures[0].AwayTeamShortName.Should().Be("TBC");
        detail.Fixtures[0].Knockout.Should().BeTrue();
    }

    [Fact]
    public async Task GetTeamDetailAsync_LeagueTable_AwardsThreePointsForAWinAndOneForADraw()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var arsenal = MakeTeam("Arsenal", "ARS");
        var chelsea = MakeTeam("Chelsea", "CHE");
        Register(dbContext, competitionId, arsenal, chelsea);

        dbContext.Match.AddRange(
            MakePlayedMatch(competitionId, arsenal, chelsea, 2, 1),
            MakePlayedMatch(competitionId, chelsea, arsenal, 1, 1));
        await dbContext.SaveChangesAsync();

        var detail = await service.GetTeamDetailAsync(competitionId, arsenal.TeamID, Guid.NewGuid());

        var table = detail!.LeagueTable!;
        var arsenalRow = table.Single(r => r.TeamID == arsenal.TeamID);
        arsenalRow.Position.Should().Be(1);
        arsenalRow.Played.Should().Be(2);
        arsenalRow.Won.Should().Be(1);
        arsenalRow.Drawn.Should().Be(1);
        arsenalRow.Lost.Should().Be(0);
        arsenalRow.GoalsFor.Should().Be(3);
        arsenalRow.GoalsAgainst.Should().Be(2);
        arsenalRow.GoalDifference.Should().Be(1);
        arsenalRow.Points.Should().Be(4);

        var chelseaRow = table.Single(r => r.TeamID == chelsea.TeamID);
        chelseaRow.Position.Should().Be(2);
        chelseaRow.Won.Should().Be(0);
        chelseaRow.Drawn.Should().Be(1);
        chelseaRow.Lost.Should().Be(1);
        chelseaRow.GoalDifference.Should().Be(-1);
        chelseaRow.Points.Should().Be(1);
    }

    [Fact]
    public async Task GetTeamDetailAsync_LeagueTable_BreaksTiesOnGoalDifferenceThenGoalsScoredThenName()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        // Each of these beats Sunderland once, so all three finish on three points; only the
        // winning margin and goals scored separate them (and team name separates the last two).
        var arsenal = MakeTeam("Arsenal", "ARS");
        var brentford = MakeTeam("Brentford", "BRE");
        var chelsea = MakeTeam("Chelsea", "CHE");
        var sunderland = MakeTeam("Sunderland", "SUN");
        Register(dbContext, competitionId, arsenal, brentford, chelsea, sunderland);

        dbContext.Match.AddRange(
            MakePlayedMatch(competitionId, arsenal, sunderland, 4, 1),
            MakePlayedMatch(competitionId, brentford, sunderland, 2, 1),
            MakePlayedMatch(competitionId, chelsea, sunderland, 1, 0));
        await dbContext.SaveChangesAsync();

        var detail = await service.GetTeamDetailAsync(competitionId, arsenal.TeamID, Guid.NewGuid());

        detail!.LeagueTable!.Select(r => r.TeamName).Should().Equal("Arsenal", "Brentford", "Chelsea", "Sunderland");
        detail.LeagueTable!.Select(r => r.Position).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public async Task GetTeamDetailAsync_LeagueTable_IncludesRegisteredTeamsThatHaveNotPlayed()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var arsenal = MakeTeam("Arsenal", "ARS");
        var chelsea = MakeTeam("Chelsea", "CHE");
        var everton = MakeTeam("Everton", "EVE");
        Register(dbContext, competitionId, arsenal, chelsea, everton);

        dbContext.Match.Add(MakePlayedMatch(competitionId, arsenal, chelsea, 1, 0));
        await dbContext.SaveChangesAsync();

        var detail = await service.GetTeamDetailAsync(competitionId, arsenal.TeamID, Guid.NewGuid());

        var evertonRow = detail!.LeagueTable!.Single(r => r.TeamID == everton.TeamID);
        evertonRow.Played.Should().Be(0);
        evertonRow.Points.Should().Be(0);
        // Pointless but with a goal difference of 0, Everton still sits above beaten Chelsea.
        evertonRow.Position.Should().Be(2);
        detail.LeagueTable!.Select(r => r.TeamName).Should().Equal("Arsenal", "Everton", "Chelsea");
    }

    [Fact]
    public async Task GetTeamDetailAsync_LeagueTable_IgnoresUnplayedMatchesAndOtherCompetitions()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var otherCompetitionId = Guid.NewGuid();
        var arsenal = MakeTeam("Arsenal", "ARS");
        var chelsea = MakeTeam("Chelsea", "CHE");
        Register(dbContext, competitionId, arsenal, chelsea);

        dbContext.Match.AddRange(
            new DomainEntities.Match { MatchID = Guid.NewGuid(), CompetitionID = competitionId, MatchDateTime = new DateTime(2026, 9, 1, 15, 0, 0), HomeTeamID = arsenal.TeamID, AwayTeamID = chelsea.TeamID },
            MakePlayedMatch(otherCompetitionId, arsenal, chelsea, 5, 0));
        await dbContext.SaveChangesAsync();

        var detail = await service.GetTeamDetailAsync(competitionId, arsenal.TeamID, Guid.NewGuid());

        detail!.LeagueTable!.Should().OnlyContain(r => r.Played == 0 && r.Points == 0);
    }

    [Fact]
    public async Task GetTeamDetailAsync_LeagueTable_IsNullWhenTheCompetitionHasKnockoutMatches()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var england = MakeTeam("England", "ENG");
        var wales = MakeTeam("Wales", "WAL");
        Register(dbContext, competitionId, england, wales);

        var groupGame = MakePlayedMatch(competitionId, england, wales, 3, 0);
        var lastSixteen = MakePlayedMatch(competitionId, wales, england, 0, 1);
        lastSixteen.Knockout = true;
        dbContext.Match.AddRange(groupGame, lastSixteen);
        await dbContext.SaveChangesAsync();

        var detail = await service.GetTeamDetailAsync(competitionId, england.TeamID, Guid.NewGuid());

        detail!.LeagueTable.Should().BeNull();
    }

    [Fact]
    public async Task GetTeamDetailAsync_ComputesGoalTotalsAndAveragesAcrossHomeAwayAndNeutralMatches()
    {
        var (dbContext, service) = MakeService();
        var competitionId = Guid.NewGuid();
        var arsenal = MakeTeam("Arsenal", "ARS");
        var chelsea = MakeTeam("Chelsea", "CHE");
        Register(dbContext, competitionId, arsenal, chelsea);

        var neutral = MakePlayedMatch(competitionId, arsenal, chelsea, 1, 1);
        neutral.NeutralGround = true;
        dbContext.Match.AddRange(
            MakePlayedMatch(competitionId, arsenal, chelsea, 3, 1),
            MakePlayedMatch(competitionId, chelsea, arsenal, 2, 0),
            neutral);
        await dbContext.SaveChangesAsync();

        var detail = await service.GetTeamDetailAsync(competitionId, arsenal.TeamID, Guid.NewGuid());

        // Home/away averages exclude the neutral-ground match; the totals include it.
        detail!.AverageGoalsForHome.Should().Be(3m);
        detail.AverageGoalsAgainstHome.Should().Be(1m);
        detail.AverageGoalsForAway.Should().Be(0m);
        detail.AverageGoalsAgainstAway.Should().Be(2m);
        detail.GoalsFor.Should().Be(4);
        detail.GoalsAgainst.Should().Be(4);
        detail.AverageGoalsForTotal.Should().BeApproximately(4m / 3m, 0.0001m);
    }
}
