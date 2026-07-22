using FluentValidation;
using Predictathon.Application.Models;

namespace Predictathon.Application.Validators;

/// <summary>
/// Validates the fields required to issue a payment credit.
/// </summary>
public class CreatePaymentCreditModelValidator : AbstractValidator<CreatePaymentCreditModel>
{
    public CreatePaymentCreditModelValidator()
    {
        RuleFor(x => x.CompetitionID)
            .NotEmpty();

        RuleFor(x => x.ExpectedUsername)
            .NotEmpty()
            .MaximumLength(50);
    }
}
