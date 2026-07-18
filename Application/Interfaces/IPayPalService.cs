namespace Predictathon.Application.Interfaces;

public record PayPalOrder(string OrderId);

public record PayPalCapture(string Status, string CaptureId, decimal AmountCaptured);

/// <summary>
/// Thin wrapper around PayPal's REST Orders v2 API (Checkout with Smart Buttons), used to take
/// entrance-fee payments. Deliberately synchronous create-then-capture, not the legacy PDT/IPN
/// redirect+webhook flow - no pending-payment bookkeeping is needed since capture happens
/// server-side, in the same request cycle as the user's approval.
/// </summary>
public interface IPayPalService
{
    /// <summary>
    /// Creates a PayPal order for the given GBP amount, returning its order id for the frontend's
    /// PayPal Buttons to render and get buyer approval for.
    /// </summary>
    Task<PayPalOrder> CreateOrderAsync(decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a buyer-approved order, returning the actual captured amount and status so the
    /// caller can verify it before treating the payment as complete.
    /// </summary>
    Task<PayPalCapture> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default);
}
