using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Turns a knockout bracket's placeholders into teams as a tournament goes on - see
/// <see cref="Services.BracketResolutionService"/> for why it proposes rather than applies.
/// </summary>
public interface IBracketResolutionService
{
    /// <summary>
    /// Every slot in a competition's bracket, with what each one is waiting on and, where there is
    /// one, the team its placeholder now resolves to. Changes nothing.
    /// </summary>
    /// <param name="competitionId">The competition whose bracket is wanted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BracketResolutionModel> GetAsync(Guid competitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fills in the slots an admin has picked. A slot that already holds a team is left alone, so
    /// applying a stale screen can't overwrite a correction someone made in the meantime.
    /// </summary>
    /// <param name="competitionId">The competition whose bracket is being filled in.</param>
    /// <param name="assignments">The slots to fill, and the team for each.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<BracketResolutionSummary>> ApplyAsync(
        Guid competitionId,
        IReadOnlyList<BracketSlotAssignment> assignments,
        CancellationToken cancellationToken = default);
}
