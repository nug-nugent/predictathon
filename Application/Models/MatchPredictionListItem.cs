namespace Predictathon.Application.Models;

/// <summary>
/// One registered competitor's prediction for a match, as returned by the MatchPredictionListGet
/// stored procedure. Property names match its output columns exactly so CallStoredProcedureAsync's
/// column-to-property mapping picks them up with no extra configuration.
/// </summary>
public class MatchPredictionListItem
{
    /// <summary>
    /// Guid.Empty when the user never predicted this match - the SP casts NULL to 0x0.
    /// </summary>
    public Guid PredictionID { get; set; }

    public string Username { get; set; } = string.Empty;

    public Guid UserID { get; set; }

    public int? HomeTeamGoals { get; set; }

    public int? AwayTeamGoals { get; set; }

    public int? Score { get; set; }
}
