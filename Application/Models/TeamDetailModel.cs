namespace Predictathon.Application.Models;

/// <summary>
/// A team's played-match stats and results within one competition, for the Team Detail page.
/// Averages follow legacy semantics: home/away averages are computed over non-neutral-ground
/// matches only, while goals-for/against totals and the total average include neutral-ground
/// matches too.
/// </summary>
public class TeamDetailModel
{
    public Guid TeamID { get; set; }

    public string TeamName { get; set; } = "";

    public string ShortName { get; set; } = "";

    /// <summary>
    /// The three-letter code shown in place of ShortName at phone widths. Null where a team
    /// has no acronym yet, and for a TBC knockout placeholder, which has no team behind it.
    /// </summary>
    public string? Acronym { get; set; }

    public string? ImageName { get; set; }

    /// <summary>
    /// The group this team is in for this competition (e.g. "Group A"), or null where the
    /// competition has no group stage. Doubles as the heading for <see cref="LeagueTable"/>, which
    /// is the group's table rather than the whole competition's whenever this is set.
    /// </summary>
    public string? GroupName { get; set; }

    public int GoalsFor { get; set; }

    public int GoalsAgainst { get; set; }

    /// <summary>Null when the team hasn't played a non-neutral home match in this competition.</summary>
    public decimal? AverageGoalsForHome { get; set; }

    public decimal? AverageGoalsAgainstHome { get; set; }

    /// <summary>Null when the team hasn't played a non-neutral away match in this competition.</summary>
    public decimal? AverageGoalsForAway { get; set; }

    public decimal? AverageGoalsAgainstAway { get; set; }

    /// <summary>Null when the team hasn't played any match in this competition.</summary>
    public decimal? AverageGoalsForTotal { get; set; }

    public decimal? AverageGoalsAgainstTotal { get; set; }

    /// <summary>The team's played matches in this competition, most recent first.</summary>
    public IReadOnlyList<MatchListItem> Results { get; set; } = [];

    /// <summary>The team's not-yet-played matches in this competition, soonest first.</summary>
    public IReadOnlyList<TeamFixtureItem> Fixtures { get; set; } = [];

    /// <summary>
    /// The table this team sits in, best-placed team first: its group's table where the team has a
    /// <see cref="GroupName"/>, otherwise the whole competition's. Null where neither applies -
    /// a competition with knockout matches but no groups, where no single table is meaningful.
    /// </summary>
    public IReadOnlyList<TeamStandingItem>? LeagueTable { get; set; }
}

/// <summary>
/// One of a team's not-yet-played matches within a competition, for the Team Detail page's
/// fixtures list.
/// </summary>
public class TeamFixtureItem
{
    public Guid MatchID { get; set; }

    public DateTime MatchDateTime { get; set; }

    public Guid? HomeTeamID { get; set; }

    /// <summary>The home team's full name, or its TBC placeholder text for an undecided knockout slot.</summary>
    public string? HomeTeam { get; set; }

    public string HomeTeamShortName { get; set; } = "";

    /// <summary>
    /// The three-letter code shown in place of HomeTeamShortName at phone widths. Null where a team
    /// has no acronym yet, and for a TBC knockout placeholder, which has no team behind it.
    /// </summary>
    public string? HomeTeamAcronym { get; set; }

    public string? HomeTeamImage { get; set; }

    public Guid? AwayTeamID { get; set; }

    /// <summary>The away team's full name, or its TBC placeholder text for an undecided knockout slot.</summary>
    public string? AwayTeam { get; set; }

    public string AwayTeamShortName { get; set; } = "";

    /// <summary>
    /// The three-letter code shown in place of AwayTeamShortName at phone widths. Null where a team
    /// has no acronym yet, and for a TBC knockout placeholder, which has no team behind it.
    /// </summary>
    public string? AwayTeamAcronym { get; set; }

    public string? AwayTeamImage { get; set; }

    public bool NeutralGround { get; set; }

    public string? Description { get; set; }

    public bool Knockout { get; set; }
}

/// <summary>
/// One row of a competition's actual football league table, built from its played matches
/// (three points for a win, one for a draw).
/// </summary>
public class TeamStandingItem
{
    /// <summary>1-based table position, assigned after ordering - ties are not shared.</summary>
    public int Position { get; set; }

    public Guid TeamID { get; set; }

    public string TeamName { get; set; } = "";

    public string ShortName { get; set; } = "";

    /// <summary>
    /// The three-letter code shown in place of ShortName at phone widths. Null where a team
    /// has no acronym yet, and for a TBC knockout placeholder, which has no team behind it.
    /// </summary>
    public string? Acronym { get; set; }

    public string? ImageName { get; set; }

    public int Played { get; set; }

    public int Won { get; set; }

    public int Drawn { get; set; }

    public int Lost { get; set; }

    public int GoalsFor { get; set; }

    public int GoalsAgainst { get; set; }

    public int GoalDifference { get; set; }

    public int Points { get; set; }
}
