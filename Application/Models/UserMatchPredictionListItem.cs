namespace Predictathon.Application.Models;

/// <summary>
/// One match in a competition week, joined with the current user's own prediction for it (if any),
/// as returned by the UserMatchPredictionListGet stored procedure. Property names match its output
/// columns exactly so CallStoredProcedureAsync's column-to-property mapping picks them up with no
/// extra configuration.
/// </summary>
public class UserMatchPredictionListItem
{
    public Guid MatchID { get; set; }

    /// <summary>
    /// Null if the current user hasn't predicted this match.
    /// </summary>
    public Guid? PredictionID { get; set; }

    public DateTime MatchDateTime { get; set; }

    /// <summary>
    /// Null for a not-yet-decided knockout placeholder (see HomeTeamTBC on the Match entity).
    /// </summary>
    public Guid? HomeTeamID { get; set; }

    public string? HomeTeam { get; set; }

    public string HomeTeamShortName { get; set; } = string.Empty;

    public string? HomeTeamImage { get; set; }

    /// <summary>
    /// Null for a not-yet-decided knockout placeholder (see AwayTeamTBC on the Match entity).
    /// </summary>
    public Guid? AwayTeamID { get; set; }

    public string? AwayTeam { get; set; }

    public string AwayTeamShortName { get; set; } = string.Empty;

    public string? AwayTeamImage { get; set; }

    /// <summary>
    /// The current user's own predicted goals, not the match result.
    /// </summary>
    public int? HomeTeamGoals { get; set; }

    public int? AwayTeamGoals { get; set; }

    public int? ActualHomeTeamGoals { get; set; }

    public int? ActualAwayTeamGoals { get; set; }

    /// <summary>
    /// Whether the match's result has been confirmed. Distinct from ActualHomeTeamGoals being
    /// non-null: a match is in play (or over but not yet processed) until this is set.
    /// </summary>
    public bool MatchPlayed { get; set; }

    /// <summary>
    /// The provisional in-play score, null until something has been heard about the match. Never a
    /// confirmed result - that's ActualHomeTeamGoals/ActualAwayTeamGoals, which stay null while a
    /// match is live no matter what this says.
    /// </summary>
    public int? LiveHomeTeamGoals { get; set; }

    /// <inheritdoc cref="LiveHomeTeamGoals" />
    public int? LiveAwayTeamGoals { get; set; }

    /// <summary>When the live score last changed - not when it was last confirmed unchanged.</summary>
    public DateTime? LiveScoreUpdatedDateTime { get; set; }

    /// <summary>
    /// When the provider was last heard from about this match, whether or not the score moved. Null
    /// for a match only ever scored by an admin. This is the honest "as at" for a reader: through a
    /// goalless spell LiveScoreUpdatedDateTime stops moving and starts reading as a stalled page.
    /// </summary>
    public DateTime? LiveScoreLastPolledDateTime { get; set; }

    public int? Score { get; set; }

    public string? Description { get; set; }

    public bool Knockout { get; set; }
}
