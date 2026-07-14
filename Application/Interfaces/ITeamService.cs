using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface ITeamService
{
    /// <summary>
    /// Gets the teams registered for a competition (via TeamCompetition), ordered by name.
    /// </summary>
    Task<IReadOnlyList<TeamModel>> GetForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default);
}
