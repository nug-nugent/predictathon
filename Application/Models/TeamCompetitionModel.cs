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
}
