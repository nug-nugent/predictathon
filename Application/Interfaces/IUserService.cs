using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Gets the publicly-viewable profile for a user, or null if no such user exists.
    /// </summary>
    Task<UserProfileModel?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
