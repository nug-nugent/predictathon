using FluentAssertions;
using Predictathon.Application.Models;
using Predictathon.Application.Validators;

namespace Predictathon.UnitTests.Validators;

public class CreateAnnouncementModelValidatorTests
{
    private readonly CreateAnnouncementModelValidator _validator = new();

    private static CreateAnnouncementModel ValidModel() => new()
    {
        Content = "The new season starts next week!",
    };

    [Fact]
    public async Task Validate_ValidModel_Passes()
    {
        var result = await _validator.ValidateAsync(ValidModel());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyContent_Fails()
    {
        var model = ValidModel();
        model.Content = "";

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAnnouncementModel.Content));
    }

    [Fact]
    public async Task Validate_ContentTooLong_Fails()
    {
        var model = ValidModel();
        model.Content = new string('x', 2001);

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAnnouncementModel.Content));
    }

    [Fact]
    public async Task Validate_NoExpiry_Passes()
    {
        var model = ValidModel();
        model.ExpiryDateTimeUtc = null;

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ExpiryInFuture_Passes()
    {
        var model = ValidModel();
        model.ExpiryDateTimeUtc = DateTime.UtcNow.AddDays(1);

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ExpiryInPast_Fails()
    {
        var model = ValidModel();
        model.ExpiryDateTimeUtc = DateTime.UtcNow.AddDays(-1);

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAnnouncementModel.ExpiryDateTimeUtc));
    }
}
