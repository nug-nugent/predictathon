using FluentAssertions;
using Predictathon.Application.Models;
using Predictathon.Application.Validators;

namespace Predictathon.UnitTests.Validators;

public class CreateCompetitionModelValidatorTests
{
    private readonly CreateCompetitionModelValidator _validator = new();

    private static CreateCompetitionModel ValidModel() => new()
    {
        CompetitionName = "World Cup",
        StartDate = new DateOnly(2026, 6, 1),
        EndDate = new DateOnly(2026, 7, 15),
        EntranceFee = 5m,
    };

    [Fact]
    public async Task Validate_ValidModel_Passes()
    {
        var result = await _validator.ValidateAsync(ValidModel());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyCompetitionName_Fails()
    {
        var model = ValidModel();
        model.CompetitionName = "";

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompetitionModel.CompetitionName));
    }

    [Fact]
    public async Task Validate_CompetitionNameTooLong_Fails()
    {
        var model = ValidModel();
        model.CompetitionName = new string('x', 201);

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompetitionModel.CompetitionName));
    }

    [Fact]
    public async Task Validate_StartDateNotBeforeEndDate_Fails()
    {
        var model = ValidModel();
        model.StartDate = new DateOnly(2026, 7, 15);
        model.EndDate = new DateOnly(2026, 7, 15);

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompetitionModel.StartDate));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public async Task Validate_EntranceFeeOutOfRange_Fails(decimal entranceFee)
    {
        var model = ValidModel();
        model.EntranceFee = entranceFee;

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCompetitionModel.EntranceFee));
    }

    [Fact]
    public async Task Validate_ZeroEntranceFee_Passes()
    {
        var model = ValidModel();
        model.EntranceFee = 0m;

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }
}
