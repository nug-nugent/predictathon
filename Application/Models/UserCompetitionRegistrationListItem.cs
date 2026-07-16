namespace Predictathon.Application.Models;

public class UserCompetitionRegistrationListItem
{
    public Guid? UserCompetitionID { get; set; }
    public Guid CompetitionID { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string? ImageFilename { get; set; }
    public DateTime StartDate { get; set; }
    public decimal EntranceFee { get; set; }
    public bool Registered { get; set; }
    public bool IsDefaultCompetition { get; set; }
}
