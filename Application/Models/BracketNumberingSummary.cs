namespace Predictathon.Application.Models;

/// <summary>
/// The outcome of numbering a competition's bracket slots from its kick-off times.
/// </summary>
public class BracketNumberingSummary
{
    /// <summary>How many matches were given a bracket slot.</summary>
    public int MatchesNumbered { get; set; }

    /// <summary>How many rounds those matches were spread across.</summary>
    public int RoundsNumbered { get; set; }
}
