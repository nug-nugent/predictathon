using FluentValidation;
using Predictathon.Application.Models;

namespace Predictathon.Application.Validators;

/// <summary>
/// Validates the fields required to create a competition. Shared with <see cref="CompetitionModelValidator"/>
/// via <see cref="AbstractValidator{T}.Include"/> so the rules aren't duplicated between create and edit.
/// </summary>
public class CreateCompetitionModelValidator : AbstractValidator<CreateCompetitionModel>
{
    public CreateCompetitionModelValidator()
    {
        RuleFor(x => x.CompetitionName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate)
            .WithMessage("StartDate must be before EndDate");

        RuleFor(x => x.EntranceFee)
            .GreaterThanOrEqualTo(0m)
            .LessThan(100m)
            .WithMessage("EntranceFee must be less than £100");
    }
}
