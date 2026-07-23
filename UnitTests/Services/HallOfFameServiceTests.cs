using FluentAssertions;
using Predictathon.Application.Errors;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class HallOfFameServiceTests
{
    private static DomainEntities.Competition MakeCompetition(Guid? competitionId = null) => new()
    {
        CompetitionID = competitionId ?? Guid.NewGuid(),
        CompetitionName = "Premier League 2025/26",
        EndDate = new DateOnly(2026, 5, 24),
        ImageFilename = "premier-league.png",
    };

    private static DomainEntities.Match MakeMatch(Guid competitionId, bool played) => new()
    {
        MatchID = Guid.NewGuid(),
        CompetitionID = competitionId,
        MatchPlayed = played,
    };

    private static LeagueTableItem MakeLeagueTableEntry(int position, string username) => new()
    {
        UserID = Guid.NewGuid(),
        Username = username,
        LeaguePosition = position,
    };

    private static (InMemoryApplicationDbContext DbContext, FakeLeagueTableService LeagueTableService, HallOfFameService Service) MakeService()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var leagueTableService = new FakeLeagueTableService();
        var service = new HallOfFameService(dbContext, dbContext, leagueTableService);
        return (dbContext, leagueTableService, service);
    }

    [Fact]
    public async Task GenerateForCompetitionAsync_CompetitionNotFound_ReturnsNotFound()
    {
        var (_, _, service) = MakeService();

        var result = await service.GenerateForCompetitionAsync(Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task GenerateForCompetitionAsync_AlreadyGenerated_ReturnsConflict()
    {
        var (dbContext, _, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        dbContext.HallOfFame.Add(new DomainEntities.HallOfFame { HallOfFameID = Guid.NewGuid(), CompetitionID = competition.CompetitionID, EndDate = competition.EndDate });
        await dbContext.SaveChangesAsync();

        var result = await service.GenerateForCompetitionAsync(competition.CompetitionID);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task GenerateForCompetitionAsync_NoMatches_ReturnsConflict()
    {
        var (dbContext, _, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        await dbContext.SaveChangesAsync();

        var result = await service.GenerateForCompetitionAsync(competition.CompetitionID);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task GenerateForCompetitionAsync_NotAllMatchesPlayed_ReturnsConflict()
    {
        var (dbContext, _, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        dbContext.Match.Add(MakeMatch(competition.CompetitionID, played: true));
        dbContext.Match.Add(MakeMatch(competition.CompetitionID, played: false));
        await dbContext.SaveChangesAsync();

        var result = await service.GenerateForCompetitionAsync(competition.CompetitionID);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task GenerateForCompetitionAsync_FewerThanThreeLeagueEntries_ReturnsConflict()
    {
        var (dbContext, leagueTableService, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        dbContext.Match.Add(MakeMatch(competition.CompetitionID, played: true));
        await dbContext.SaveChangesAsync();
        leagueTableService.Table = [MakeLeagueTableEntry(1, "Alice"), MakeLeagueTableEntry(2, "Bob")];

        var result = await service.GenerateForCompetitionAsync(competition.CompetitionID);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task GenerateForCompetitionAsync_Success_CreatesEntryFromTopThreeLeaguePositions()
    {
        var (dbContext, leagueTableService, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        dbContext.Match.Add(MakeMatch(competition.CompetitionID, played: true));
        await dbContext.SaveChangesAsync();
        var first = MakeLeagueTableEntry(1, "Alice");
        var second = MakeLeagueTableEntry(2, "Bob");
        var third = MakeLeagueTableEntry(3, "Carol");
        leagueTableService.Table = [first, second, third, MakeLeagueTableEntry(4, "Dave")];

        var result = await service.GenerateForCompetitionAsync(competition.CompetitionID);

        result.IsSuccess.Should().BeTrue();
        result.Value.Winner.Should().Be("Alice");
        result.Value.WinnerUserID.Should().Be(first.UserID);
        result.Value.SecondPlace.Should().Be("Bob");
        result.Value.SecondPlaceUserID.Should().Be(second.UserID);
        result.Value.ThirdPlace.Should().Be("Carol");
        result.Value.ThirdPlaceUserID.Should().Be(third.UserID);
        result.Value.CompetitionName.Should().Be(competition.CompetitionName);
        result.Value.EndDate.Should().Be(competition.EndDate);
        result.Value.ImageFilename.Should().Be(competition.ImageFilename);

        var saved = dbContext.HallOfFame.Should().ContainSingle().Subject;
        saved.CompetitionID.Should().Be(competition.CompetitionID);
        saved.WinnerUserID.Should().Be(first.UserID);
    }

    [Fact]
    public async Task GetGenerationStatusAsync_NoMatches_AllMatchesPlayedIsFalse()
    {
        var (dbContext, _, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        await dbContext.SaveChangesAsync();

        var status = await service.GetGenerationStatusAsync(competition.CompetitionID);

        status.AllMatchesPlayed.Should().BeFalse();
        status.AlreadyGenerated.Should().BeFalse();
    }

    [Fact]
    public async Task GetGenerationStatusAsync_AllMatchesPlayedAndAlreadyGenerated_ReflectsBoth()
    {
        var (dbContext, _, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        dbContext.Match.Add(MakeMatch(competition.CompetitionID, played: true));
        dbContext.HallOfFame.Add(new DomainEntities.HallOfFame { HallOfFameID = Guid.NewGuid(), CompetitionID = competition.CompetitionID, EndDate = competition.EndDate });
        await dbContext.SaveChangesAsync();

        var status = await service.GetGenerationStatusAsync(competition.CompetitionID);

        status.AllMatchesPlayed.Should().BeTrue();
        status.AlreadyGenerated.Should().BeTrue();
    }
}
