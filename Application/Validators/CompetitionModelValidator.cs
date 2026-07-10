using FluentValidation;
using Predictathon.Application.Models;

namespace Predictathon.Application.Validators;

public class CompetitionModelValidator : AbstractValidator<CompetitionModel>
{
    public CompetitionModelValidator()
    {
        Include(new CreateCompetitionModelValidator());
    }
}
