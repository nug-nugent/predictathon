using FluentValidation;
using Predictathon.Application.Models;

namespace Predictathon.Application.Validators;

/// <summary>
/// Validates the fields required to edit an announcement. Doesn't share
/// <see cref="CreateAnnouncementModelValidator"/>'s expiry-must-be-future rule - see that class for why.
/// </summary>
public class AnnouncementModelValidator : AbstractValidator<AnnouncementModel>
{
    public AnnouncementModelValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
