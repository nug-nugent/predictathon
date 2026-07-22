namespace Predictathon.Application.Models;

/// <summary>
/// Model used to capture a buyer-approved PayPal order when registering for a competition.
/// </summary>
public class CapturePayPalOrderModel
{
    public string OrderId { get; set; } = string.Empty;
}
