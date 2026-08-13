namespace Predictathon.WebApi.Options;

/// <summary>
/// Rate limit settings for the unauthenticated auth endpoints (login, register, forgot-password,
/// reset-password), bound from the "AuthRateLimit" configuration section.
/// </summary>
public sealed class AuthRateLimitOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "AuthRateLimit";

    /// <summary>Maximum number of requests permitted per client IP within <see cref="WindowSeconds"/>.</summary>
    public int PermitLimit { get; init; } = 30;

    /// <summary>Length, in seconds, of the fixed window <see cref="PermitLimit"/> applies to.</summary>
    public int WindowSeconds { get; init; } = 60;
}
