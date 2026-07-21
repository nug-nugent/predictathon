using FluentAssertions;
using Predictathon.Application.Models;
using Predictathon.Application.Validators;

namespace Predictathon.UnitTests.Validators;

public class CreatePaymentCreditModelValidatorTests
{
    private readonly CreatePaymentCreditModelValidator _validator = new();

    private static CreatePaymentCreditModel ValidModel() => new()
    {
        CompetitionID = Guid.NewGuid(),
        ExpectedUsername = "dave",
    };

    [Fact]
    public async Task Validate_ValidModel_Passes()
    {
        var result = await _validator.ValidateAsync(ValidModel());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyCompetitionID_Fails()
    {
        var model = ValidModel();
        model.CompetitionID = Guid.Empty;

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentCreditModel.CompetitionID));
    }

    [Fact]
    public async Task Validate_EmptyExpectedUsername_Fails()
    {
        var model = ValidModel();
        model.ExpectedUsername = "";

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentCreditModel.ExpectedUsername));
    }

    [Fact]
    public async Task Validate_ExpectedUsernameTooLong_Fails()
    {
        var model = ValidModel();
        model.ExpectedUsername = new string('a', 51);

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePaymentCreditModel.ExpectedUsername));
    }
}
