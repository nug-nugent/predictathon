namespace Predictathon.Application.Models;

/// <summary>
/// Matches HallOfFameListGet's output - Winner/SecondPlace/ThirdPlace prefer the linked user's
/// current username, falling back to the plain-text name stored on the row for entries that
/// predate (or were never linked to) an actual user account.
/// </summary>
public class HallOfFameListItem
{
    public Guid HallOfFameID { get; set; }

    public Guid? CompetitionID { get; set; }

    public string? CompetitionName { get; set; }

    public DateOnly EndDate { get; set; }

    public string? ImageFilename { get; set; }

    public string? Winner { get; set; }

    public Guid? WinnerUserID { get; set; }

    public string? SecondPlace { get; set; }

    public Guid? SecondPlaceUserID { get; set; }

    public string? ThirdPlace { get; set; }

    public Guid? ThirdPlaceUserID { get; set; }
}
