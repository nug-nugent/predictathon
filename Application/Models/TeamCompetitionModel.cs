namespace Predictathon.Application.Models;

/// <summary>
/// A team assigned to a competition, for competition-admin team management (includes the join id
/// needed to remove the assignment, unlike <see cref="TeamModel"/>).
/// </summary>
public class TeamCompetitionModel
{
    public Guid TeamCompetitionID { get; set; }

    public Guid TeamID { get; set; }

    public string TeamName { get; set; } = "";

    /// <summary>
    /// The group this team is drawn into for the competition (e.g. "Group A"), or null where the
    /// competition has no group stage or the team hasn't been placed in a group yet.
    /// </summary>
    public string? GroupName { get; set; }
}
