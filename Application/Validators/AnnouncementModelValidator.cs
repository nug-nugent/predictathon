using FluentValidation;
using Predictathon.Application.Constants;
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

        RuleFor(x => x)
            .Must(x => x.ShowOnLoginPage || x.ShowOnHomepage)
            .WithMessage("Select at least one of \"Show on login page\" or \"Show on homepage\".")
            .WithName("ShowOnLoginPage");

        RuleFor(x => x.Severity)
            .Must(s => s is AnnouncementSeverities.Info or AnnouncementSeverities.Warning)
            .WithMessage("Severity must be either \"Info\" or \"Warning\".");
    }
}
