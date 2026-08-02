using FluentAssertions;
using Predictathon.Application.Models;
using Predictathon.Application.Validators;

namespace Predictathon.UnitTests.Validators;

public class AnnouncementModelValidatorTests
{
    private readonly AnnouncementModelValidator _validator = new();

    private static AnnouncementModel ValidModel() => new()
    {
        AnnouncementID = 1,
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AnnouncementModel.Content));
    }

    [Fact]
    public async Task Validate_ExpiryAlreadyInPast_StillPasses()
    {
        // The whole reason AnnouncementModelValidator doesn't share CreateAnnouncementModelValidator's
        // expiry-must-be-future rule: editing an already-expired announcement (e.g. to fix a typo, or
        // clear its expiry) must stay possible without also requiring the expiry to be bumped forward.
        var model = ValidModel();
        model.ExpiryDateTimeUtc = DateTime.UtcNow.AddDays(-1);

        var result = await _validator.ValidateAsync(model);

        result.IsValid.Should().BeTrue();
    }
}
