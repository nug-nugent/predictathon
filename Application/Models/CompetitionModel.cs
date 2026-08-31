namespace Predictathon.Application.Models;

/// <summary>
/// Model used when creating a competition.
/// </summary>
public class CreateCompetitionModel
{
    public string CompetitionName { get; set; } = "";

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

    /// <summary>
    /// The competition series this competition belongs to, if any - what groups repeated wins in
    /// the same competition into a single counted trophy on a profile. Null for a one-off, which
    /// still earns its own trophy, just an ungrouped one.
    /// </summary>
    public Guid? CompetitionSeriesID { get; set; }

    /// <summary>
    /// The external fixture data source's competition code (e.g. "PL" for football-data.org's
    /// Premier League), used by fixture import/sync. Null if this competition has no external source.
    /// </summary>
    public string? ExternalApiCompetitionCode { get; set; }
}

// Full model including the generated identifier.
public class CompetitionModel : CreateCompetitionModel
{
    public Guid CompetitionID { get; set; }
}
