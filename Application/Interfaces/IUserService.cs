using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Gets the publicly-viewable profile for a user, or null if no such user exists.
    /// </summary>
    Task<UserProfileModel?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full editable profile for a user, or null if no such user exists.
    /// </summary>
    Task<UserProfileEditModel?> GetProfileForEditAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's profile (Identity.Users only). <paramref name="allowAdminFields"/> gates
    /// whether CanViewMessageboard/CanViewHiddenMessageThreads are persisted - callers should pass
    /// true only when the caller (not necessarily the profile's owner) holds UserAdministrator.
    /// </summary>
    Task<Result<UserProfileEditModel>> UpdateProfileAsync(Guid userId, UpdateProfileModel model, bool allowAdminFields, CancellationToken cancellationToken = default);
}
