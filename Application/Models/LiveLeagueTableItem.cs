namespace Predictathon.Application.Models;

/// <summary>
/// One row of the Live page's league table: the standing as it would be right now if every match in
/// play ended on its current scoreline. Every inherited column - points, goal difference, the
/// pointer counts, the position - already has the live scores applied, and
/// <see cref="LeagueTableItem.PreviousLeaguePosition"/> carries where the user stands on confirmed
/// results alone, so the position-change arrow reads the way it does everywhere else: how you got to
/// the row you're on.
/// </summary>
public class LiveLeagueTableItem : LeagueTableItem
{
    /// <summary>
    /// How much of <see cref="LeagueTableItem.Score"/> comes from matches still in play, and is
    /// therefore provisional. Zero when the user has no prediction on anything live, or their
    /// predictions aren't scoring against the scoreline so far.
    /// </summary>
    public int LivePoints { get; set; }
}
