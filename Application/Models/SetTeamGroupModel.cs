namespace Predictathon.Application.Models;

/// <summary>
/// Request body for placing a team already assigned to a competition into one of its groups.
/// </summary>
public class SetTeamGroupModel
{
    /// <summary>
    /// The group to place the team in (e.g. "Group A"), or null/blank to take it out of a group
    /// again. Stored as typed, since it doubles as the group table's heading.
    /// </summary>
    public string? GroupName { get; set; }
}
