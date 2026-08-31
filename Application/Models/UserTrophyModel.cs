namespace Predictathon.Application.Models;

/// <summary>
/// Matches UserTrophiesGet's output - one trophy a user has won. Wins within the same competition
/// series collapse into a single row carrying <see cref="WinCount"/>; a win in a competition with
/// no series stays its own row, named after that competition.
/// </summary>
public class UserTrophyModel
{
    public Guid UserID { get; set; }

    /// <summary>
    /// The series this trophy belongs to, or null for a one-off competition outside any series.
    /// </summary>
    public Guid? CompetitionSeriesID { get; set; }

    /// <summary>
    /// The series name, or the competition's own name for a one-off.
    /// </summary>
    public string Name { get; set; } = "";

    public string? ShortName { get; set; }

    public string? BadgeIcon { get; set; }

    public string? BadgeColour { get; set; }

    public int DisplayOrder { get; set; }

    public int WinCount { get; set; }

    public DateOnly MostRecentWin { get; set; }

    /// <summary>
    /// The years won, oldest first, comma separated - e.g. "2010, 2014, 2022".
    /// </summary>
    public string Years { get; set; } = "";
}
