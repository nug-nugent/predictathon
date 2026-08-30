namespace Predictathon.Application.Models;

/// <summary>
/// What one pass of the live-score refresh did, for the polling service to log. Nothing acts on
/// this - it exists so an operator reading the log can tell "nothing was in play" apart from
/// "something was in play and we couldn't reach the provider".
/// </summary>
public class LiveScoreRefreshSummary
{
    /// <summary>Matches that were in play and eligible for a score fetch.</summary>
    public int MatchesInPlay { get; set; }

    /// <summary>Matches whose stored scoreline changed as a result of this pass.</summary>
    public int ScoresChanged { get; set; }

    /// <summary>
    /// Set when the pass deliberately made no provider call - the rate-limit budget was spent, or
    /// another worker process had already polled inside this interval.
    /// </summary>
    public string? SkippedReason { get; set; }
}
