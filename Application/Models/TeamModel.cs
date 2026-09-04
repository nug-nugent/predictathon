namespace Predictathon.Application.Models;

public class TeamModel
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
}
