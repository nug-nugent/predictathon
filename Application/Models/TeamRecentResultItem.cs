namespace Predictathon.Application.Models;

/// <summary>
/// One of a team's played matches within a competition, for the recent-results popup opened from a
/// team's name. <see cref="Outcome"/> is from the point of view of the team the results were asked
/// for; everything else describes the match itself, laid out home team first like a fixture.
/// </summary>
public class TeamRecentResultItem
{
    public Guid MatchID { get; set; }

    public DateTime MatchDateTime { get; set; }

    public Guid? HomeTeamID { get; set; }

    /// <summary>The home team's full name, or its TBC placeholder text for an undecided knockout slot.</summary>
    public string? HomeTeam { get; set; }

    public string HomeTeamShortName { get; set; } = "";

    public string? HomeTeamImage { get; set; }

    public Guid? AwayTeamID { get; set; }

    /// <summary>The away team's full name, or its TBC placeholder text for an undecided knockout slot.</summary>
    public string? AwayTeam { get; set; }

    public string AwayTeamShortName { get; set; } = "";

    public string? AwayTeamImage { get; set; }

    public int HomeTeamGoals { get; set; }

    public int AwayTeamGoals { get; set; }

    public bool NeutralGround { get; set; }

    public string? Description { get; set; }

    public bool Knockout { get; set; }

    /// <summary>"Win", "Draw" or "Loss" for the team whose recent results these are.</summary>
    public string Outcome { get; set; } = "";
}
