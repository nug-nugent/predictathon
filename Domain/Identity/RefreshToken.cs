namespace Predictathon.Domain.Identity;

/// <summary>
/// A server-side record of an issued refresh token. Only the SHA-256 hash of the raw token is
/// stored (same principle as password hashing) - a database dump alone doesn't yield usable tokens.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public byte[] TokenHash { get; set; } = [];

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}
