namespace Predictathon.Application.Models;

public class TeamModel
{
    public Guid TeamID { get; set; }

    public string TeamName { get; set; } = "";

    public string ShortName { get; set; } = "";

    public string? ImageName { get; set; }
}
