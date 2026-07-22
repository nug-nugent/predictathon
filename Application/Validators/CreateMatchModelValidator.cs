using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;

namespace Predictathon.Application.Validators;

/// <summary>
/// Validates the fields required to create a match, including a duplicate-fixture check that
/// respects the competition's <c>DuplicateFixturesAllowed</c> flag. Not shared with
/// <see cref="MatchModelValidator"/> via <see cref="AbstractValidator{T}.Include"/> - the duplicate
/// check needs to exclude the match's own id when editing, which the shared base type has no id to do.
/// </summary>
public class CreateMatchModelValidator : AbstractValidator<CreateMatchModel>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateMatchModelValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(x => x.CompetitionID)
            .NotEmpty();

        RuleFor(x => x.MatchDateTime)
            .NotEqual(default(DateTime))
            .WithMessage("A valid match date and time is required.");

        RuleFor(x => x)
            .Must(x => x.HomeTeamID.HasValue || !string.IsNullOrWhiteSpace(x.HomeTeamTBC))
            .WithMessage("Select a home team or enter a placeholder name.")
            .WithName("HomeTeamID");

        RuleFor(x => x)
            .Must(x => x.AwayTeamID.HasValue || !string.IsNullOrWhiteSpace(x.AwayTeamTBC))
            .WithMessage("Select an away team or enter a placeholder name.")
            .WithName("AwayTeamID");

        RuleFor(x => x)
            .MustAsync((model, ct) => IsNotDuplicateFixtureAsync(model.CompetitionID, model.HomeTeamID, model.AwayTeamID, excludeMatchId: null, ct))
            .WithMessage("A match between these teams already exists in this competition.");
    }

    protected async Task<bool> IsNotDuplicateFixtureAsync(Guid competitionId, Guid? homeTeamId, Guid? awayTeamId, Guid? excludeMatchId, CancellationToken cancellationToken)
    {
        if (!homeTeamId.HasValue || !awayTeamId.HasValue)
        {
            return true;
        }

        var competition = await _dbContext.Competition.FirstOrDefaultAsync(c => c.CompetitionID == competitionId, cancellationToken);
        if (competition is null || competition.DuplicateFixturesAllowed)
        {
            return true;
        }

        var duplicateExists = await _dbContext.Match.AnyAsync(m =>
            m.CompetitionID == competitionId &&
            m.HomeTeamID == homeTeamId &&
            m.AwayTeamID == awayTeamId &&
            m.MatchID != (excludeMatchId ?? Guid.Empty),
            cancellationToken);

        return !duplicateExists;
    }
}
