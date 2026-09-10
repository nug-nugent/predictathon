namespace Predictathon.Application.Models;

/// <summary>
/// What one auto-processing sweep did, for the service that drives it to log. Nothing acts on this -
/// it exists so an operator reading the log can tell a quiet sweep ("the provider hasn't called
/// anything finished") apart from one that found finished matches and couldn't confirm them.
/// </summary>
public class ResultAutoProcessSummary
{
    /// <summary>Matches whose result was confirmed from the provider's final score by this sweep.</summary>
    public int ResultsProcessed { get; set; }

    /// <summary>
    /// Matches the provider has called finished but which couldn't be confirmed, so they're still
    /// waiting on an admin. Non-zero is worth a look: the usual cause is the eligibility rule in
    /// <see cref="Interfaces.IMatchService.SaveResultAsync"/> refusing the save.
    /// </summary>
    public int ResultsFailed { get; set; }

    /// <summary>
    /// Set when the sweep deliberately did nothing at all - auto-processing is switched off in
    /// configuration.
    /// </summary>
    public string? SkippedReason { get; set; }
}
