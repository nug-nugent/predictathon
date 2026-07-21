using FluentAssertions;
using Predictathon.Application.Models;
using Predictathon.Application.Validators;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Validators;

public class CreateMatchModelValidatorTests
{
    private static CreateMatchModel ValidModel(Guid competitionId, Guid? homeTeamId, Guid? awayTeamId) => new()
    {
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

        var validator = new CreateMatchModelValidator(dbContext);
        var model = ValidModel(competitionId, Guid.NewGuid(), Guid.NewGuid());

        var result = await validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyCompetitionID_Fails()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var validator = new CreateMatchModelValidator(dbContext);
        var model = ValidModel(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        var result = await validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMatchModel.CompetitionID));
    }

    [Fact]
    public async Task Validate_DefaultMatchDateTime_Fails()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var validator = new CreateMatchModelValidator(dbContext);
        var model = ValidModel(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        model.MatchDateTime = default;

        var result = await validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMatchModel.MatchDateTime));
    }

    [Fact]
    public async Task Validate_NoHomeTeamOrPlaceholder_Fails()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var validator = new CreateMatchModelValidator(dbContext);
        var model = ValidModel(Guid.NewGuid(), null, Guid.NewGuid());
        model.HomeTeamID = null;
        model.HomeTeamTBC = null;

        var result = await validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HomeTeamID");
    }

    [Fact]
    public async Task Validate_HomeTeamPlaceholderInsteadOfId_Passes()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var competitionId = Guid.NewGuid();
        dbContext.Competition.Add(new DomainEntities.Competition { CompetitionID = competitionId, CompetitionName = "Test" });
        await dbContext.SaveChangesAsync();

        var validator = new CreateMatchModelValidator(dbContext);
        var model = ValidModel(competitionId, null, Guid.NewGuid());
        model.HomeTeamID = null;
        model.HomeTeamTBC = "Winner of Group A";

        var result = await validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NoAwayTeamOrPlaceholder_Fails()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var validator = new CreateMatchModelValidator(dbContext);
        var model = ValidModel(Guid.NewGuid(), Guid.NewGuid(), null);
        model.AwayTeamID = null;
        model.AwayTeamTBC = null;

        var result = await validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AwayTeamID");
    }

    [Fact]
    public async Task Validate_DuplicateFixture_WhenNotAllowed_Fails()
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

        var validator = new CreateMatchModelValidator(dbContext);
        var model = ValidModel(competitionId, homeTeamId, awayTeamId);

        var result = await validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_DuplicateFixture_WhenAllowed_Passes()
    {
        await using var dbContext = new InMemoryApplicationDbContext();
        var competitionId = Guid.NewGuid();
        var homeTeamId = Guid.NewGuid();
        var awayTeamId = Guid.NewGuid();
        dbContext.Competition.Add(new DomainEntities.Competition
        {
            CompetitionID = competitionId,
            CompetitionName = "Test",
            DuplicateFixturesAllowed = true,
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

        var validator = new CreateMatchModelValidator(dbContext);
        var model = ValidModel(competitionId, homeTeamId, awayTeamId);

        var result = await validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }
}
