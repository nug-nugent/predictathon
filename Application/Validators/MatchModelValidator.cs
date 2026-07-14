using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;

namespace Predictathon.Application.Validators;

/// <summary>
/// Validates edits to an existing match. Not built via <see cref="AbstractValidator{T}.Include"/>
/// from <see cref="CreateMatchModelValidator"/> - the duplicate-fixture check here needs to exclude
/// the match's own id, which the shared create validator has no id to do.
/// </summary>
public class MatchModelValidator : AbstractValidator<MatchModel>
{
    private readonly IApplicationDbContext _dbContext;

    public MatchModelValidator(IApplicationDbContext dbContext)
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
            .MustAsync((model, ct) => IsNotDuplicateFixtureAsync(model.CompetitionID, model.HomeTeamID, model.AwayTeamID, model.MatchID, ct))
            .WithMessage("A match between these teams already exists in this competition.");
    }

    private async Task<bool> IsNotDuplicateFixtureAsync(Guid competitionId, Guid? homeTeamId, Guid? awayTeamId, Guid excludeMatchId, CancellationToken cancellationToken)
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
            m.MatchID != excludeMatchId,
            cancellationToken);

        return !duplicateExists;
    }
}
