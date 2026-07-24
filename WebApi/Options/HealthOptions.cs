namespace Predictathon.WebApi.Options;

/// <summary>
/// Health check endpoint settings, bound from the "Health" configuration section.
/// </summary>
public sealed class HealthOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "Health";

    /// <summary>
    /// Shared secret required via the "X-Api-Key" request header to call the basic "/health"
    /// endpoint. When null or empty, that endpoint is left unprotected.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Shared secret required via the "X-Api-Key" request header to call "/health/detailed",
    /// separate from <see cref="ApiKey"/> since it reveals per-check names and timings that the
    /// basic endpoint deliberately withholds. When null or empty, that endpoint is left
    /// unprotected.
    /// </summary>
    public string? DetailedApiKey { get; init; }
}
