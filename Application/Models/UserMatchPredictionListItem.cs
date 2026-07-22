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

    public int? Score { get; set; }

    public string? Description { get; set; }

    public bool Knockout { get; set; }
}
