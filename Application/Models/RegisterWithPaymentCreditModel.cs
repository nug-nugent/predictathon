namespace Predictathon.Application.Models;

/// <summary>
/// Model used to redeem a payment credit code when registering for a competition.
/// </summary>
public class RegisterWithPaymentCreditModel
{
    public string Code { get; set; } = string.Empty;
}
