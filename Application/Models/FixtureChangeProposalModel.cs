namespace Predictathon.Application.Models;

/// <summary>
/// A pending fixture reschedule detected against an external data source, awaiting admin review.
/// </summary>
public class FixtureChangeProposalModel
{
    public int FixtureChangeProposalID { get; set; }

    public Guid MatchID { get; set; }

    public string HomeTeamName { get; set; } = "";

    public string AwayTeamName { get; set; } = "";

    public DateTime PreviousMatchDateTime { get; set; }

    public DateTime ProposedMatchDateTime { get; set; }

    public DateTime DetectedAtUtc { get; set; }
}
