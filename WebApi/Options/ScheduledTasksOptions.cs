namespace Predictathon.WebApi.Options;

/// <summary>
/// Scheduled task endpoint settings, bound from the "ScheduledTasks" configuration section.
/// </summary>
public sealed class ScheduledTasksOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "ScheduledTasks";

    /// <summary>
    /// Shared secret required via the "X-Api-Key" request header to call the scheduled task
    /// endpoints. When null or empty, they're left unprotected (matches legacy behaviour).
    /// </summary>
    public string? ApiKey { get; init; }
}
