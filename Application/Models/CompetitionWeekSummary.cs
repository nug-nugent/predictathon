namespace Predictathon.Application.Models;

/// <summary>
/// One Friday-starting match week of a competition, summarised for the calling user. Used by the
/// Predictions page to decide which week to land on and to flag weeks with outstanding predictions.
/// </summary>
public class CompetitionWeekSummary
{
    /// <summary>
    /// The Friday the week starts on.
    /// </summary>
    public DateTime WeekStart { get; set; }

    /// <summary>
    /// Kick-off of the latest match in the week. Lets the client decide whether the week is still
    /// open using its own save cutoff, rather than trusting a server-computed flag that goes stale
    /// the moment it's serialised.
    /// </summary>
    public DateTime LastMatchDateTime { get; set; }

    /// <summary>
    /// Matches in the week the user hasn't predicted and can still predict (kick-off in the future).
    /// </summary>
    public int OpenUnpredictedCount { get; set; }
}
