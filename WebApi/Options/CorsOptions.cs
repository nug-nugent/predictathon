namespace Predictathon.WebApi.Options;

/// <summary>
/// Cross-origin resource sharing settings, bound from the "Cors" configuration section.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "Cors";

    /// <summary>
    /// Origins allowed to call the API with credentials. Empty by default, so nothing is
    /// permitted until explicitly configured - there is no permissive "just for dev" fallback.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];
}
