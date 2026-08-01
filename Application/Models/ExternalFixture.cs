namespace Predictathon.Application.Models;

/// <summary>
/// A single fixture as reported by an external match-data provider, translated into
/// provider-agnostic shape by the <see cref="Interfaces.IExternalMatchDataService"/> implementation.
/// </summary>
public class ExternalFixture
{
    /// <summary>The provider's own identifier for this fixture.</summary>
    public int ExternalMatchID { get; set; }

    /// <summary>The fixture's current scheduled kickoff time, in UTC.</summary>
    public DateTime KickoffUtc { get; set; }

    /// <summary>The provider's identifier for the home team.</summary>
    public string HomeTeamExternalCode { get; set; } = "";

    /// <summary>The provider's identifier for the away team.</summary>
    public string AwayTeamExternalCode { get; set; } = "";

    /// <summary>The home team's name, as reported by the provider.</summary>
    public string HomeTeamName { get; set; } = "";

    /// <summary>The away team's name, as reported by the provider.</summary>
    public string AwayTeamName { get; set; } = "";
}
