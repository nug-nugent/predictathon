namespace Predictathon.Application.Options;

/// <summary>
/// football-data.org API settings, bound from the "FootballDataApi" configuration section. Lives in
/// Application (rather than alongside WebApi/Options, like most other options types) because it's
/// consumed by an Application-layer service, which can't reference the WebApi project.
/// </summary>
public sealed class FootballDataApiOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "FootballDataApi";

    /// <summary>The free-tier API key, sent as the X-Auth-Token header on every request.</summary>
    public string ApiKey { get; init; } = "";
}
