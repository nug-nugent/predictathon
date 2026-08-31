namespace Predictathon.Application.Models;

/// <summary>
/// Request to record or correct a match's provisional in-play score. Not a result: recording the
/// real one, and scoring predictions against it, is <see cref="SaveMatchResultRequest"/>'s job.
/// </summary>
public class SaveLiveScoreRequest
{
    public int HomeTeamGoals { get; set; }

    public int AwayTeamGoals { get; set; }
}
