using FluentAssertions;
using Predictathon.Application.Common;
using Predictathon.Application.Errors;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class FixtureImportServiceTests
{
    private static DomainEntities.Competition MakeCompetition(string? externalApiCompetitionCode = "PL") => new()
    {
        CompetitionID = Guid.NewGuid(),
        CompetitionName = "Premier League 2026/27",
        StartDate = new DateOnly(2026, 8, 1),
        EndDate = new DateOnly(2026, 8, 2),
        ExternalApiCompetitionCode = externalApiCompetitionCode,
    };

    private static DomainEntities.Team MakeTeam(string name, string externalApiCode) => new()
    {
        TeamID = Guid.NewGuid(),
        TeamName = name,
        ShortName = name,
        ExternalApiCode = externalApiCode,
    };

    private static ExternalFixture MakeFixture(int externalMatchId, DateTime kickoffUtc, string homeCode, string awayCode) => new()
    {
        ExternalMatchID = externalMatchId,
        KickoffUtc = kickoffUtc,
        HomeTeamExternalCode = homeCode,
        AwayTeamExternalCode = awayCode,
        HomeTeamName = "Home Team",
        AwayTeamName = "Away Team",
    };

    private static (InMemoryApplicationDbContext DbContext, FakeExternalMatchDataService ExternalMatchDataService, FixtureImportService Service) MakeService()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var externalMatchDataService = new FakeExternalMatchDataService();
        var service = new FixtureImportService(dbContext, externalMatchDataService);
        return (dbContext, externalMatchDataService, service);
    }

    [Fact]
    public async Task ImportSeasonAsync_CompetitionNotFound_ReturnsNotFound()
    {
        var (_, _, service) = MakeService();

        var result = await service.ImportSeasonAsync(Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task ImportSeasonAsync_NoExternalCompetitionCode_ReturnsConflict()
    {
        var (dbContext, _, service) = MakeService();
        var competition = MakeCompetition(externalApiCompetitionCode: null);
        dbContext.Competition.Add(competition);
        await dbContext.SaveChangesAsync();

        var result = await service.ImportSeasonAsync(competition.CompetitionID);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task ImportSeasonAsync_NoFixturesReturned_ReturnsConflict()
    {
        var (dbContext, externalMatchDataService, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        await dbContext.SaveChangesAsync();
        externalMatchDataService.Fixtures = [];

        var result = await service.ImportSeasonAsync(competition.CompetitionID);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task ImportSeasonAsync_TeamNotMappedToExternalCode_ReturnsConflict()
    {
        var (dbContext, externalMatchDataService, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        await dbContext.SaveChangesAsync();
        // No Team rows have a matching ExternalApiCode for "57"/"61".
        externalMatchDataService.Fixtures = [MakeFixture(100, new DateTime(2026, 8, 15, 14, 0, 0, DateTimeKind.Utc), "57", "61")];

        var result = await service.ImportSeasonAsync(competition.CompetitionID);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task ImportSeasonAsync_Success_CreatesMatchesAndTeamCompetitionsAndRefinesCompetitionDates()
    {
        var (dbContext, externalMatchDataService, service) = MakeService();
        var competition = MakeCompetition();
        var arsenal = MakeTeam("Arsenal", "57");
        var chelsea = MakeTeam("Chelsea", "61");
        dbContext.Competition.Add(competition);
        dbContext.Team.Add(arsenal);
        dbContext.Team.Add(chelsea);
        await dbContext.SaveChangesAsync();

        var firstKickoffUtc = new DateTime(2026, 8, 15, 14, 0, 0, DateTimeKind.Utc);
        var secondKickoffUtc = new DateTime(2026, 8, 22, 14, 0, 0, DateTimeKind.Utc);
        externalMatchDataService.Fixtures =
        [
            MakeFixture(100, firstKickoffUtc, "57", "61"),
            MakeFixture(101, secondKickoffUtc, "61", "57"),
        ];

        var result = await service.ImportSeasonAsync(competition.CompetitionID);

        result.IsSuccess.Should().BeTrue();
        result.Value.MatchesImported.Should().Be(2);
        result.Value.TeamsAdded.Should().Be(2);
        result.Value.StartDate.Should().Be(DateOnly.FromDateTime(UkClock.ToUkLocal(firstKickoffUtc)));
        result.Value.EndDate.Should().Be(DateOnly.FromDateTime(UkClock.ToUkLocal(secondKickoffUtc)));

        dbContext.Match.Should().HaveCount(2);
        var firstMatch = dbContext.Match.Should().ContainSingle(m => m.ExternalMatchID == 100).Subject;
        firstMatch.HomeTeamID.Should().Be(arsenal.TeamID);
        firstMatch.AwayTeamID.Should().Be(chelsea.TeamID);
        firstMatch.MatchDateTime.Should().Be(UkClock.ToUkLocal(firstKickoffUtc));

        dbContext.TeamCompetition.Should().HaveCount(2);
        dbContext.TeamCompetition.Should().Contain(tc => tc.TeamID == arsenal.TeamID && tc.CompetitionID == competition.CompetitionID);
        dbContext.TeamCompetition.Should().Contain(tc => tc.TeamID == chelsea.TeamID && tc.CompetitionID == competition.CompetitionID);
    }

    [Fact]
    public async Task ImportSeasonAsync_CalledTwice_IsIdempotent()
    {
        var (dbContext, externalMatchDataService, service) = MakeService();
        var competition = MakeCompetition();
        var arsenal = MakeTeam("Arsenal", "57");
        var chelsea = MakeTeam("Chelsea", "61");
        dbContext.Competition.Add(competition);
        dbContext.Team.Add(arsenal);
        dbContext.Team.Add(chelsea);
        await dbContext.SaveChangesAsync();

        externalMatchDataService.Fixtures = [MakeFixture(100, new DateTime(2026, 8, 15, 14, 0, 0, DateTimeKind.Utc), "57", "61")];

        await service.ImportSeasonAsync(competition.CompetitionID);
        var secondResult = await service.ImportSeasonAsync(competition.CompetitionID);

        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.MatchesImported.Should().Be(0);
        secondResult.Value.TeamsAdded.Should().Be(0);
        dbContext.Match.Should().HaveCount(1);
        dbContext.TeamCompetition.Should().HaveCount(2);
    }
}
