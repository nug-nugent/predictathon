using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Manages payment credit codes - single-use codes a UserAdministrator issues to let a specific
/// user register for a competition without paying its entrance fee, for the Payment Credit Admin
/// page. Redemption of these codes is handled by <see cref="IUserCompetitionService"/>.
/// </summary>
public interface IPaymentCreditService
{
    /// <summary>
    /// Gets every payment credit across all competitions, most recently issued first.
    /// </summary>
    Task<IReadOnlyList<PaymentCreditListItem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a new payment credit, generating its unique redemption code. Fails with a
    /// <see cref="Errors.NotFoundError"/> if the competition doesn't exist.
    /// </summary>
    Task<Result<PaymentCreditListItem>> CreateAsync(CreatePaymentCreditModel model, Guid issuedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a payment credit, whether or not it has been used. Fails with a
    /// <see cref="Errors.NotFoundError"/> if it doesn't exist.
    /// </summary>
    Task<Result> DeleteAsync(Guid paymentCreditId, CancellationToken cancellationToken = default);
}
