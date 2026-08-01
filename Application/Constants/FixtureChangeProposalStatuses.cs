namespace Predictathon.Application.Constants;

/// <summary>
/// FixtureChangeProposal.Status values. Stored as plain text (mirroring Transaction.TransactionStatus)
/// rather than a lookup table, since there are only ever these three fixed values.
/// </summary>
public static class FixtureChangeProposalStatuses
{
    public const string Pending = "Pending";

    public const string Confirmed = "Confirmed";

    public const string Dismissed = "Dismissed";
}
