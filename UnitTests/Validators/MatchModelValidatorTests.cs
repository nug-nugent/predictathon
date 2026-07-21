using FluentAssertions;
using Predictathon.Application.Models;
using Predictathon.Application.Validators;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Validators;

public class MatchModelValidatorTests
{
    private static MatchModel ValidModel(Guid matchId, Guid competitionId, Guid homeTeamId, Guid awayTeamId) => new()
    {
        MatchID = matchId,
        CompetitionID = competitionId,
        MatchDateTime = new DateTime(2026, 6, 1, 15, 0, 0),
        HomeTeamID = homeTeamId,
        AwayTeamID = awayTeamId,
    };

    [Fact]
    public async Task Validate_ValidModel_Passes()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var competitionId = Guid.NewGuid();
        dbContext.Competition.Add(new DomainEntities.Competition { CompetitionID = competitionId, CompetitionName = "Test" });
        await dbContext.SaveChangesAsync();

        var validator = new MatchModelValidator(dbContext);
        var model = ValidModel(Guid.NewGuid(), competitionId, Guid.NewGuid(), Guid.NewGuid());

        var result = await validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_DuplicateFixture_ExcludesOwnMatchId()
    {
        // The whole reason MatchModelValidator exists separately from CreateMatchModelValidator:
        // editing a match shouldn't flag itself as a duplicate of itself.
        await using var dbContext = new InMemoryApplicationDbContext();
        var competitionId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var homeTeamId = Guid.NewGuid();
        var awayTeamId = Guid.NewGuid();
        dbContext.Competition.Add(new DomainEntities.Competition
        {
            CompetitionID = competitionId,
            CompetitionName = "Test",
            DuplicateFixturesAllowed = false,
        });
        dbContext.Match.Add(new DomainEntities.Match
        {
            MatchID = matchId,
            CompetitionID = competitionId,
            HomeTeamID = homeTeamId,
            AwayTeamID = awayTeamId,
            MatchDateTime = new DateTime(2026, 1, 1),
        });
        await dbContext.SaveChangesAsync();

        var validator = new MatchModelValidator(dbContext);
        var model = ValidModel(matchId, competitionId, homeTeamId, awayTeamId);

        var result = await validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_DuplicateFixture_AgainstAnotherMatch_Fails()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var competitionId = Guid.NewGuid();
        var homeTeamId = Guid.NewGuid();
        var awayTeamId = Guid.NewGuid();
        dbContext.Competition.Add(new DomainEntities.Competition
        {
            CompetitionID = competitionId,
            CompetitionName = "Test",
            DuplicateFixturesAllowed = false,
        });
        dbContext.Match.Add(new DomainEntities.Match
        {
            MatchID = Guid.NewGuid(),
            CompetitionID = competitionId,
            HomeTeamID = homeTeamId,
            AwayTeamID = awayTeamId,
            MatchDateTime = new DateTime(2026, 1, 1),
        });
        await dbContext.SaveChangesAsync();

        var validator = new MatchModelValidator(dbContext);
        var model = ValidModel(Guid.NewGuid(), competitionId, homeTeamId, awayTeamId);

        var result = await validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
    }
}
