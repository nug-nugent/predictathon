using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface ITeamService
{
    /// <summary>
    /// Gets the teams registered for a competition (via TeamCompetition), ordered by name.
    /// </summary>
    Task<IReadOnlyList<TeamModel>> GetForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the teams assigned to a competition including their TeamCompetitionID, ordered by name
    /// (for competition-admin team management, where the join id is needed to remove an assignment).
    /// </summary>
    Task<IReadOnlyList<TeamCompetitionModel>> GetAssignedForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets teams not yet assigned to a competition, ordered by name (for populating an "add team" selector).
    /// </summary>
    Task<IReadOnlyList<TeamModel>> GetUnassignedForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a team to a competition.
    /// </summary>
    Task<Result> AddToCompetitionAsync(Guid competitionId, Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a team's assignment from a competition.
    /// </summary>
    Task<Result> RemoveFromCompetitionAsync(Guid teamCompetitionId, CancellationToken cancellationToken = default);
}
