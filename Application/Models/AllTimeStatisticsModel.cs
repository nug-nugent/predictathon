namespace Predictathon.Application.Models;

/// <summary>
/// One row from Statistics_CompetitionWinnerListGet - overall Hall of Fame wins/2nds/3rds per user.
/// </summary>
public class CompetitionWinnerListItem
{
    public Guid UserID { get; set; }

    public string Username { get; set; } = "";

    public int Wins { get; set; }

    public int SecondPlaces { get; set; }

    public int ThirdPlaces { get; set; }
}

/// <summary>
/// One row from Statistics_HighestAllTimeScoreListGet - top 10 total all-time points per predictor.
/// </summary>
public class HighestAllTimeScoreListItem
{
    public Guid UserID { get; set; }

    public string Username { get; set; } = "";

    public int TotalScore { get; set; }
}

/// <summary>
/// One row from Statistics_HighestAverageScorePerPredictionsGet - top 10 average points per prediction.
/// </summary>
public class HighestAverageScoreListItem
{
    public Guid UserID { get; set; }

    public string Username { get; set; } = "";

    public decimal AverageScore { get; set; }
}

/// <summary>
/// One row from Statistics_HighestPercentageCorrectPredictionsGet - top 10 by percentage of
/// predictions scoring any points (win/lose/draw correct).
/// </summary>
public class HighestPercentageCorrectListItem
{
    public Guid UserID { get; set; }

    public string Username { get; set; } = "";

    public decimal CorrectPredictionPercentage { get; set; }
}

/// <summary>
/// One row from Statistics_MostMatchesPredictedUserListGet - top 10 by total predictions made.
/// </summary>
public class MostPredictionsListItem
{
    public Guid UserID { get; set; }

    public string Username { get; set; } = "";

    public int TotalPredictions { get; set; }
}

/// <summary>
/// Every all-time (not competition-scoped) statistics widget shown on the Statistics page.
/// </summary>
public class AllTimeStatisticsModel
{
    public IReadOnlyList<CompetitionWinnerListItem> CompetitionWinners { get; set; } = [];

    public IReadOnlyList<HighestAllTimeScoreListItem> HighestAllTimeScores { get; set; } = [];

    public IReadOnlyList<HighestAverageScoreListItem> HighestAverageScores { get; set; } = [];

    public IReadOnlyList<HighestPercentageCorrectListItem> HighestPercentageCorrect { get; set; } = [];

    public IReadOnlyList<MostPredictionsListItem> MostPredictions { get; set; } = [];
}
