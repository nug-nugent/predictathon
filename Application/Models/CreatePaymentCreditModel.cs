namespace Predictathon.Application.Models;

/// <summary>
/// Model used to issue a new payment credit code for a competition.
/// </summary>
public class CreatePaymentCreditModel
{
    public Guid CompetitionID { get; set; }

    public string ExpectedUsername { get; set; } = string.Empty;
}
