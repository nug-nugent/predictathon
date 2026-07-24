namespace Predictathon.WebApi.Options;

/// <summary>
/// Scheduled background task settings, bound from the "ScheduledTasks" configuration section.
/// </summary>
public sealed class ScheduledTasksOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "ScheduledTasks";

    /// <summary>The time of day (UTC) the daily scheduled tasks should run at.</summary>
    public TimeSpan RunAtUtc { get; init; } = TimeSpan.FromHours(6);
}
