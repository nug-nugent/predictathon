namespace Predictathon.Application.Models;

/// <summary>
/// One group's table, plus whether the group has finished playing.
///
/// The "finished" part is what the bracket's resolution needs and a team's page does not: a group
/// two matches from the end has a leader, but it does not yet have a winner, and filling "Winner
/// Group A" from it would be a guess dressed up as a fact.
/// </summary>
public class GroupStandingsModel
{
    /// <summary>The group's name, as drawn - "Group A".</summary>
    public string GroupName { get; set; } = "";

    /// <summary>The group's table, best first, ordered by the competition's tie-break rule.</summary>
    public IReadOnlyList<TeamStandingItem> Standings { get; set; } = [];

    /// <summary>How many of the group's matches are still to be played.</summary>
    public int MatchesRemaining { get; set; }

    /// <summary>Whether every match in the group has been played, so the table is final.</summary>
    public bool IsComplete => MatchesRemaining == 0;
}
