namespace Predictathon.Application.Models;

/// <summary>
/// Request to record a match's final score.
/// </summary>
public class SaveMatchResultRequest
{
    public Guid MatchID { get; set; }

    public int HomeTeamGoals { get; set; }

    public int AwayTeamGoals { get; set; }
}
