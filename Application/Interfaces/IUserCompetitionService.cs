using FluentResults;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Registers a user for a competition (creates the <see cref="Domain.Entities.UserCompetition"/> row),
/// covering the entrance-fee-free and payment-credit-redemption paths. PayPal registration will be
/// added here alongside the free/credit paths.
/// </summary>
public interface IUserCompetitionService
{
    /// <summary>
    /// Registers a user for a competition that has no entrance fee. Fails with a
    /// <see cref="Errors.NotFoundError"/> if the competition doesn't exist, or a
    /// <see cref="Errors.ConflictError"/> if it isn't open for registration, has an entrance fee,
    /// or the user is already registered.
    /// </summary>
    Task<Result> RegisterFreeAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a user for a competition by redeeming a single-use payment credit code. Fails with
    /// a <see cref="Errors.NotFoundError"/> if the competition doesn't exist, a
    /// <see cref="Errors.PropertyValidationError"/> if the code doesn't exist or isn't valid for this
    /// competition, or a <see cref="Errors.ConflictError"/> if the competition isn't open for
    /// registration, the code has already been used, or the user is already registered.
    /// </summary>
    Task<Result> RegisterWithPaymentCreditAsync(Guid userId, Guid competitionId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a PayPal order for a competition's entrance fee, for the frontend's PayPal Buttons
    /// to get buyer approval for. Fails with a <see cref="Errors.NotFoundError"/> if the
    /// competition doesn't exist, or a <see cref="Errors.ConflictError"/> if it isn't open for
    /// registration, doesn't accept PayPal, or has no entrance fee.
    /// </summary>
    Task<Result<PayPalOrder>> CreatePayPalOrderAsync(Guid competitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a buyer-approved PayPal order and, once confirmed complete and for the correct
    /// amount, registers the user for the competition. Fails with a
    /// <see cref="Errors.NotFoundError"/> if the competition doesn't exist, or a
    /// <see cref="Errors.ConflictError"/> if the user is already registered, PayPal didn't confirm
    /// the payment as complete, or the captured amount doesn't match the entrance fee.
    /// </summary>
    Task<Result> CapturePayPalOrderAsync(Guid userId, Guid competitionId, string orderId, CancellationToken cancellationToken = default);
}
