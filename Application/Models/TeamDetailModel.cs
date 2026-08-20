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

    public string? ImageName { get; set; }

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
}
