using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IHallOfFameService
{
    /// <summary>
    /// Gets every Hall of Fame entry, most recently concluded competition first.
    /// </summary>
    Task<IReadOnlyList<HallOfFameListItem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether a competition is currently eligible to have its Hall of Fame entry auto-generated.
    /// </summary>
    /// <param name="competitionId">The competition to check.</param>
    Task<HallOfFameGenerationStatus> GetGenerationStatusAsync(Guid competitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a competition's Hall of Fame entry (1st/2nd/3rd place) from its live league table.
    /// Fails with a <see cref="Errors.NotFoundError"/> if the competition doesn't exist, or a
    /// <see cref="Errors.ConflictError"/> if it already has a Hall of Fame entry, its matches aren't
    /// all played yet, or fewer than 3 users have a league position to award.
    /// </summary>
    /// <param name="competitionId">The competition to generate the entry for.</param>
    Task<Result<HallOfFameListItem>> GenerateForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default);
}
