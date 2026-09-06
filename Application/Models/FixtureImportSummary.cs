namespace Predictathon.Application.Models;

/// <summary>
/// The outcome of importing a competition's season fixtures from an external data source.
/// </summary>
public class FixtureImportSummary
{
    /// <summary>How many new Match rows were created (already-imported fixtures are skipped).</summary>
    public int MatchesImported { get; set; }

    /// <summary>How many new TeamCompetition rows were created.</summary>
    public int TeamsAdded { get; set; }

    /// <summary>
    /// How many already-assigned teams had their group filled in from the imported fixtures. Teams
    /// added by this same import are counted under <see cref="TeamsAdded"/>, group and all.
    /// </summary>
    public int GroupsAssigned { get; set; }

    /// <summary>The competition's resolved start date, refined from the imported fixtures.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>The competition's resolved end date, refined from the imported fixtures.</summary>
    public DateOnly EndDate { get; set; }
}
