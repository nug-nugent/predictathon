namespace Predictathon.Application.Interfaces;

public interface IRefreshTokenService
{
    /// <summary>
    /// Creates and persists a new refresh token for the given user, returning the raw token.
    /// The raw value is never stored - only its hash - so it must be captured here; it cannot
    /// be recovered later.
    /// </summary>
    Task<string> GenerateAsync(Guid userId, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the owning user's id if <paramref name="rawToken"/> matches an active
    /// (non-revoked, non-expired) refresh token, otherwise null.
    /// </summary>
    Task<Guid?> ValidateAsync(string rawToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the refresh token matching <paramref name="rawToken"/>, if any. Idempotent -
    /// does nothing if the token doesn't exist or is already revoked.
    /// </summary>
    Task RevokeAsync(string rawToken, CancellationToken cancellationToken = default);
}
