using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Reads the trophies users have won, derived from the Hall of Fame rather than stored separately.
/// </summary>
public interface ITrophyService
{
    /// <summary>
    /// Gets one user's trophies, best-known series first.
    /// </summary>
    /// <param name="userId">The user whose trophies to get.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<UserTrophyModel>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trophies for several users at once, keyed by user. Users with no wins are absent from
    /// the dictionary rather than present with an empty list.
    /// </summary>
    /// <param name="userIds">The users whose trophies to get.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyDictionary<Guid, List<UserTrophyModel>>> GetForUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
