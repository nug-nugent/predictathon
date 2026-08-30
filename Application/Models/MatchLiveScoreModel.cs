namespace Predictathon.Application.Models;

/// <summary>
/// A match's current live score - provisional, and not to be confused with the confirmed result on
/// the Match itself (see dbo.MatchLiveScore).
/// </summary>
public class MatchLiveScoreModel
{
    public Guid MatchID { get; set; }

    public int HomeTeamGoals { get; set; }

    public int AwayTeamGoals { get; set; }

    /// <summary>The provider's own status vocabulary, or null for an admin-entered score.</summary>
    public string? Status { get; set; }

    /// <summary>See <see cref="Constants.LiveScoreSource"/>.</summary>
    public string Source { get; set; } = "";

    /// <summary>When the scoreline last changed - not when it was last confirmed unchanged.</summary>
    public DateTime UpdatedDateTime { get; set; }
}
