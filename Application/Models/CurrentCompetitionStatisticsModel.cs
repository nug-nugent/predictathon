namespace Predictathon.Application.Models;

/// <summary>
/// One row from AverageScoreByTeamListGet - a team's average prediction score across the
/// competition's played matches.
/// </summary>
public class PredictableTeamListItem
{
    public Guid TeamID { get; set; }

    public string ShortName { get; set; } = "";

    /// <summary>
    /// The three-letter code shown in place of ShortName at phone widths. Null where a team
    /// has no acronym yet, and for a TBC knockout placeholder, which has no team behind it.
    /// </summary>
    public string? Acronym { get; set; }

    public string TeamName { get; set; } = "";

    public string? TeamImage { get; set; }

    public decimal AverageScore { get; set; }
}

/// <summary>
/// One row from MatchResultListGet - a played match with the current user's own prediction plus
/// the average prediction score across all users. Used for the Results and Match Detail pages, a
/// team's result history, and to rank matches as most/least predictable on the Statistics page.
/// </summary>
public class MatchListItem
{
    public Guid MatchID { get; set; }

    public DateTime MatchDateTime { get; set; }

    public Guid? HomeTeamID { get; set; }

    public string? HomeTeam { get; set; }

    public string HomeTeamShortName { get; set; } = "";

    /// <summary>
    /// The three-letter code shown in place of HomeTeamShortName at phone widths. Null where a team
    /// has no acronym yet, and for a TBC knockout placeholder, which has no team behind it.
    /// </summary>
    public string? HomeTeamAcronym { get; set; }

    public string? HomeTeamImage { get; set; }

    public Guid? AwayTeamID { get; set; }

    public string? AwayTeam { get; set; }

    public string AwayTeamShortName { get; set; } = "";

    /// <summary>
    /// The three-letter code shown in place of AwayTeamShortName at phone widths. Null where a team
    /// has no acronym yet, and for a TBC knockout placeholder, which has no team behind it.
    /// </summary>
    public string? AwayTeamAcronym { get; set; }

    public string? AwayTeamImage { get; set; }

    public int? HomeTeamGoals { get; set; }

    public int? AwayTeamGoals { get; set; }

    public int? PredictionHomeTeamGoals { get; set; }

    public int? PredictionAwayTeamGoals { get; set; }

    public int YourPredictionScore { get; set; }

    public decimal AveragePredictionScore { get; set; }

    public string? Description { get; set; }

    public bool Knockout { get; set; }
}

/// <summary>
/// One row from MatchPredictionAverageBiggestDifferencesGet - a single user's prediction that beat
/// the average prediction score for that match, ordered by how much it beat it by.
/// </summary>
public class BestPredictionListItem
{
    public Guid UserID { get; set; }

    public string Username { get; set; } = "";

    public Guid MatchID { get; set; }

    public DateTime MatchDateTime { get; set; }

    public string? HomeTeam { get; set; }

    public string HomeTeamShortName { get; set; } = "";

    /// <summary>
    /// The three-letter code shown in place of HomeTeamShortName at phone widths. Null where a team
    /// has no acronym yet, and for a TBC knockout placeholder, which has no team behind it.
    /// </summary>
    public string? HomeTeamAcronym { get; set; }

    public string? AwayTeam { get; set; }

    public string AwayTeamShortName { get; set; } = "";

    /// <summary>
    /// The three-letter code shown in place of AwayTeamShortName at phone widths. Null where a team
    /// has no acronym yet, and for a TBC knockout placeholder, which has no team behind it.
    /// </summary>
    public string? AwayTeamAcronym { get; set; }

    public int? HomeTeamGoals { get; set; }

    public int? AwayTeamGoals { get; set; }

    public int? PredictionHomeTeamGoals { get; set; }

    public int? PredictionAwayTeamGoals { get; set; }

    public int PredictionScore { get; set; }

    public decimal AveragePredictionScore { get; set; }

    public decimal ScoreDifference { get; set; }

    public string? Description { get; set; }

    public bool Knockout { get; set; }
}

/// <summary>
/// Every current-competition-scoped statistics widget shown on the Statistics page.
/// </summary>
public class CurrentCompetitionStatisticsModel
{
    public IReadOnlyList<PredictableTeamListItem> PredictableTeams { get; set; } = [];

    public IReadOnlyList<MatchListItem> MostPredictableMatches { get; set; } = [];

    public IReadOnlyList<MatchListItem> LeastPredictableMatches { get; set; } = [];

    public IReadOnlyList<BestPredictionListItem> BestPredictions { get; set; } = [];
}
