namespace Predictathon.Application.Models;

/// <summary>
/// A created PayPal order, for the frontend's PayPal Buttons to render and get buyer approval for.
/// </summary>
public class PayPalOrderModel
{
    public string OrderId { get; set; } = string.Empty;
}
