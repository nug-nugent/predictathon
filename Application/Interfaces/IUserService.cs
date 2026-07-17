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

    /// <summary>
    /// Gets a server-paged, optionally search-filtered list of users for the User Admin page.
    /// <paramref name="search"/>, when supplied, matches (case-insensitively) against username,
    /// email, forename, or surname.
    /// </summary>
    Task<PagedResult<UserAdminListItem>> GetUsersForAdminAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the full role set a user should hold. <paramref name="currentUserId"/> is the caller -
    /// used to refuse a UserAdministrator removing their own UserAdministrator role, which would
    /// leave them locked out of this page with no other admin able to undo it.
    /// </summary>
    Task<Result> UpdateUserRolesAsync(Guid userId, IReadOnlyList<string> roles, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks or unlocks a user's account. <paramref name="currentUserId"/> is the caller - used to
    /// refuse locking your own account.
    /// </summary>
    Task<Result> SetUserLockedAsync(Guid userId, bool locked, Guid currentUserId, CancellationToken cancellationToken = default);
}
