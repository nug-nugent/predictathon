using FluentValidation;
using Predictathon.Application.Models;

namespace Predictathon.Application.Validators;

public class CompetitionModelValidator : AbstractValidator<CompetitionModel>
{
    public CompetitionModelValidator()
    {
        RuleFor(x => x.CompetitionName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate)
            .WithMessage("StartDate must be before EndDate");

        RuleFor(x => x.EntranceFee)
            .GreaterThanOrEqualTo(0m);
    }
}
