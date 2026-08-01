namespace Predictathon.WebApi.Options;

/// <summary>
/// Fixture-change-detection background task settings, bound from the "FixtureSync" configuration section.
/// </summary>
public sealed class FixtureSyncOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "FixtureSync";

    /// <summary>How often the fixture sync job runs. Defaults to every 4 hours.</summary>
    public int IntervalHours { get; init; } = 4;
}
