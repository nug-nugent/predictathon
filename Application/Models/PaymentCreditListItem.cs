namespace Predictathon.Application.Models;

/// <summary>
/// A single row of the Payment Credit Admin list.
/// </summary>
public class PaymentCreditListItem
{
    public Guid PaymentCreditID { get; set; }

    public string? ExpectedUsername { get; set; }

    public Guid CompetitionID { get; set; }

    public string CompetitionName { get; set; } = string.Empty;

    public string UniquePaymentCode { get; set; } = string.Empty;

    public bool CreditUsed { get; set; }

    public DateTime IssueDate { get; set; }
}
