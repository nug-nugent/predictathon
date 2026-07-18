namespace Predictathon.Application.Models;

/// <summary>
/// A competition open for registration, shown to a logged-out visitor on the pre-login landing
/// page alongside a link into <c>/register?competitionId=</c>.
/// </summary>
public class CompetitionRegistrationSummaryModel
{
    public Guid CompetitionID { get; set; }

    public string CompetitionName { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal EntranceFee { get; set; }

    public string? Information { get; set; }
}
