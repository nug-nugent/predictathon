namespace Predictathon.Application.Models;

/// <summary>
/// A single match's current score as reported by an external match-data provider, translated into
/// provider-agnostic shape by the <see cref="Interfaces.IExternalMatchDataService"/> implementation.
/// </summary>
public class ExternalMatchScore
{
    /// <summary>The provider's own identifier for this fixture.</summary>
    public int ExternalMatchID { get; set; }

    /// <summary>
    /// The provider's own status vocabulary (e.g. "IN_PLAY", "PAUSED", "FINISHED"). Passed through
    /// rather than mapped onto an enum of our own - see the note on dbo.MatchLiveScore.
    /// </summary>
    public string Status { get; set; } = "";

    /// <summary>
    /// Goals so far, or null for a fixture the provider has no score for yet (not started, or
    /// postponed). A match in play with no goals reports 0, not null.
    /// </summary>
    public int? HomeTeamGoals { get; set; }

    /// <inheritdoc cref="HomeTeamGoals" />
    public int? AwayTeamGoals { get; set; }

    /// <summary>Whether the provider considers this match over, and its score final.</summary>
    public bool IsFinished => string.Equals(Status, FinishedStatus, StringComparison.OrdinalIgnoreCase);

    /// <summary>The provider's status value for a completed match.</summary>
    public const string FinishedStatus = "FINISHED";
}
