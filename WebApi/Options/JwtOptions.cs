namespace Predictathon.WebApi.Options;

/// <summary>
/// JWT bearer authentication settings, bound from the "Jwt" configuration section.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "Jwt";

    /// <summary>The token issuer, validated against incoming tokens' iss claim.</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>The token audience, validated against incoming tokens' aud claim.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Symmetric key used to sign and validate access tokens.</summary>
    public string SigningKey { get; init; } = string.Empty;
}
