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

    /// <summary>
    /// Root of the provider's API, without a trailing slash. Configurable so a version bump or a
    /// move to their paid host doesn't need a rebuild, and so tests can point at a stub.
    /// </summary>
    public string BaseUrl { get; init; } = "https://api.football-data.org/v4";

    /// <summary>
    /// How many requests the provider's plan allows per minute. The free tier's limit is 10; it's
    /// configuration rather than a constant so a plan change is an appsettings edit, not a release.
    /// Every call through the client is counted against this, so an admin running fixture imports
    /// and the live-score poller share one budget.
    /// </summary>
    public int RequestsPerMinute { get; init; } = 10;

    /// <summary>
    /// How many of that minute's requests to leave unused. Guards the case where IIS briefly runs
    /// two worker processes side by side during an overlapped recycle: each has its own in-process
    /// counter, so without headroom the pair can exceed the provider's limit between them.
    /// </summary>
    public int RequestsPerMinuteHeadroom { get; init; } = 2;

    /// <summary>How often to ask the provider for scores while at least one match is in play.</summary>
    public int LiveScorePollSeconds { get; init; } = 60;

    /// <summary>
    /// Swaps the real provider for a simulated one that invents plausible in-play scores from the
    /// fixtures already in the database. For the Docker dev stack, which has no API key and whose
    /// sample fixtures don't exist at football-data.org - without it the live-score feature can't be
    /// seen working locally at all. Never honoured in Production (see Program.cs).
    /// </summary>
    public bool UseSimulatedProvider { get; init; }
}
