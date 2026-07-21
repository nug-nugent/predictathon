using FluentAssertions;
using Predictathon.Application.Models;
using Predictathon.Application.Validators;

namespace Predictathon.UnitTests.Validators;

public class UpdateProfileModelValidatorTests
{
    private readonly UpdateProfileModelValidator _validator = new();

    private static UpdateProfileModel ValidModel() => new()
    {
        UserName = "dave",
        Email = "dave@example.com",
    };

    [Fact]
    public async Task Validate_ValidModel_Passes()
    {
        var result = await _validator.ValidateAsync(ValidModel());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmailShapedUserName_Fails()
    {
        var model = ValidModel();
        model.UserName = "dave@example.com";

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileModel.UserName));
    }

    [Fact]
    public async Task Validate_InvalidEmail_Fails()
    {
        var model = ValidModel();
        model.Email = "not-an-email";

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileModel.Email));
    }

    [Fact]
    public async Task Validate_ForenamesTooLong_Fails()
    {
        var model = ValidModel();
        model.Forenames = new string('a', 51);

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileModel.Forenames));
    }

    [Fact]
    public async Task Validate_NullOptionalFields_Passes()
    {
        var model = ValidModel();
        model.Forenames = null;
        model.Surname = null;
        model.FavouriteTeam = null;
        model.Location = null;
        model.Caption = null;
        model.EmailPredictionReminderDays = null;

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmailPredictionReminderDaysZero_Fails()
    {
        var model = ValidModel();
        model.EmailPredictionReminderDays = 0;

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileModel.EmailPredictionReminderDays));
    }

    [Fact]
    public async Task Validate_EmailPredictionReminderDaysPositive_Passes()
    {
        var model = ValidModel();
        model.EmailPredictionReminderDays = 3;

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }
}
