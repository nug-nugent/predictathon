namespace Predictathon.Application.Models;

public class CompetitionModel
{
    public Guid CompetitionID { get; set; }

    public required string CompetitionName { get; set; }

    public bool PrependNameWithThe { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool DuplicateFixturesAllowed { get; set; }

    public bool OpenForRegistration { get; set; }

    public bool RegistrationAvailableOnLoginPage { get; set; }

    public bool ShowInHallOfFame { get; set; }

    public decimal EntranceFee { get; set; }

    public bool PayPalPaymentAvailable { get; set; }

    public string? Information { get; set; }

    public string? ImageFilename { get; set; }

    public bool DefaultToNeutralGround { get; set; }

    public bool AllowTwoPointers { get; set; }
}