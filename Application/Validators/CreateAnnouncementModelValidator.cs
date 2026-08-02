using FluentValidation;
using Predictathon.Application.Models;

namespace Predictathon.Application.Validators;

/// <summary>
/// Validates the fields required to create an announcement. The expiry-must-be-future rule is
/// deliberately not shared with <see cref="AnnouncementModelValidator"/> - editing an already-expired
/// announcement (e.g. to fix a typo, or clear its expiry) must stay possible without also requiring
/// the expiry to be bumped forward.
/// </summary>
public class CreateAnnouncementModelValidator : AbstractValidator<CreateAnnouncementModel>
{
    public CreateAnnouncementModelValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.ExpiryDateTimeUtc)
            .GreaterThan(_ => DateTime.UtcNow)
            .When(x => x.ExpiryDateTimeUtc.HasValue)
            .WithMessage("ExpiryDateTimeUtc must be in the future.");
    }
}
