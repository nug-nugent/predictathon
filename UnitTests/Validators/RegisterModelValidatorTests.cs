using FluentAssertions;
using Predictathon.Application.Models;
using Predictathon.Application.Validators;

namespace Predictathon.UnitTests.Validators;

public class RegisterModelValidatorTests
{
    private readonly RegisterModelValidator _validator = new();

    private static RegisterModel ValidModel() => new()
    {
        UserName = "dave",
        Email = "dave@example.com",
        Password = "password1",
        Forenames = "David",
        Surname = "Huggett",
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterModel.UserName));
    }

    [Fact]
    public async Task Validate_UserNameTooLong_Fails()
    {
        var model = ValidModel();
        model.UserName = new string('a', 257);

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterModel.UserName));
    }

    [Fact]
    public async Task Validate_InvalidEmail_Fails()
    {
        var model = ValidModel();
        model.Email = "not-an-email";

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterModel.Email));
    }

    [Fact]
    public async Task Validate_PasswordTooShort_Fails()
    {
        var model = ValidModel();
        model.Password = "short1";

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterModel.Password));
    }

    [Theory]
    [InlineData("")]
    public async Task Validate_EmptyForenames_Fails(string forenames)
    {
        var model = ValidModel();
        model.Forenames = forenames;

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterModel.Forenames));
    }

    [Fact]
    public async Task Validate_EmptySurname_Fails()
    {
        var model = ValidModel();
        model.Surname = "";

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterModel.Surname));
    }
}
