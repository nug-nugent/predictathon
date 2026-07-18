using System.Text.RegularExpressions;
using FluentValidation;
using Predictathon.Application.Models;

namespace Predictathon.Application.Validators;

public class RegisterModelValidator : AbstractValidator<RegisterModel>
{
    // Deliberately simple - just enough to catch "this is clearly an email address", not a full
    // RFC 5322 email validator (FluentValidation's EmailAddress() rule already covers Email itself).
    private static readonly Regex EmailLikePattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public RegisterModelValidator()
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

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.Forenames)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Surname)
            .NotEmpty()
            .MaximumLength(50);
    }
}
