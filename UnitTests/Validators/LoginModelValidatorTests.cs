using FluentAssertions;
using Predictathon.Application.Models;
using Predictathon.Application.Validators;

namespace Predictathon.UnitTests.Validators;

public class LoginModelValidatorTests
{
    private readonly LoginModelValidator _validator = new();

    [Fact]
    public async Task Validate_ValidModel_Passes()
    {
        var model = new LoginModel { UserName = "dave", Password = "hunter2" };

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyUserName_Fails()
    {
        var model = new LoginModel { UserName = "", Password = "hunter2" };

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginModel.UserName));
    }

    [Fact]
    public async Task Validate_EmptyPassword_Fails()
    {
        var model = new LoginModel { UserName = "dave", Password = "" };

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginModel.Password));
    }
}
