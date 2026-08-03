namespace Predictathon.Application.Models;

/// <summary>
/// Site-wide stats shown on the pre-login landing page.
/// </summary>
public class PublicStatsModel
{
    public int PredictionsMadeCount { get; set; }

    public int CompletedCompetitionsCount { get; set; }
}
