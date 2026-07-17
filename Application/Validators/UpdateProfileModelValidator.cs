using System.Text.RegularExpressions;
using FluentValidation;
using Predictathon.Application.Models;

namespace Predictathon.Application.Validators;

public class UpdateProfileModelValidator : AbstractValidator<UpdateProfileModel>
{
    // Deliberately simple - just enough to catch "this is clearly an email address", not a full
    // RFC 5322 email validator (FluentValidation's EmailAddress() rule already covers Email itself).
    private static readonly Regex EmailLikePattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public UpdateProfileModelValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(256)
            // Login falls back to FindByEmailAsync when a username lookup fails, so an
            // email-shaped username is genuinely ambiguous at login time - reject it up front.
            .Must(userName => !EmailLikePattern.IsMatch(userName))
            .WithMessage("Username must not be an email address.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Forenames).MaximumLength(50);
        RuleFor(x => x.Surname).MaximumLength(50);
        RuleFor(x => x.FavouriteTeam).MaximumLength(50);
        RuleFor(x => x.Location).MaximumLength(50);
        RuleFor(x => x.Caption).MaximumLength(30);

        RuleFor(x => x.EmailPredictionReminderDays)
            .GreaterThan(0)
            .When(x => x.EmailPredictionReminderDays is not null);
    }
}
