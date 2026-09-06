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

    /// <summary>
    /// Whether <see cref="KickoffUtc"/> is a real, broadcaster-confirmed kickoff time. Some providers
    /// report a placeholder time (e.g. midnight UTC) for fixtures whose exact slot hasn't been
    /// confirmed yet - defaults to <c>true</c> so providers that don't report this distinction at all
    /// are treated as always-confirmed.
    /// </summary>
    public bool IsKickoffConfirmed { get; set; } = true;

    /// <summary>The provider's identifier for the home team.</summary>
    public string HomeTeamExternalCode { get; set; } = "";

    /// <summary>The provider's identifier for the away team.</summary>
    public string AwayTeamExternalCode { get; set; } = "";

    /// <summary>The home team's name, as reported by the provider.</summary>
    public string HomeTeamName { get; set; } = "";

    /// <summary>The away team's name, as reported by the provider.</summary>
    public string AwayTeamName { get; set; } = "";

    /// <summary>
    /// The group this fixture belongs to (e.g. "Group A"), for a tournament with a group stage.
    /// Null for a fixture with no group - every fixture in a league season, and the knockout rounds
    /// of a tournament.
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Whether this fixture is part of a knockout round rather than a group stage or league season.
    /// </summary>
    public bool IsKnockout { get; set; }

    /// <summary>
    /// The round or stage this fixture belongs to, in the form the site shows it (e.g. "Group A",
    /// "Quarter final"), or null where the provider reports nothing useful.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Which knockout round this fixture is in - see <c>dbo.Match.KnockoutRound</c> and
    /// <see cref="Common.KnockoutRounds"/> - or null where it isn't a knockout fixture. The bracket
    /// position within the round isn't derivable from what providers report, so it stays null and an
    /// admin numbers the draw by hand.
    /// </summary>
    public int? KnockoutRound { get; set; }
}
