namespace Predictathon.Application.Models;

/// <summary>
/// Request to upsert the current user's prediction for a match.
/// </summary>
public class SavePredictionRequest
{
    public Guid MatchID { get; set; }

    public int HomeTeamGoals { get; set; }

    public int AwayTeamGoals { get; set; }
}
