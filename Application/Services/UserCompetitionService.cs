using FluentResults;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Services;

[ScopedService]
public class UserCompetitionService : IUserCompetitionService
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IPayPalService _payPalService;

    public UserCompetitionService(IApplicationDbContext appDbContext, IPayPalService payPalService)
    {
        _appDbContext = appDbContext ?? throw new ArgumentNullException(nameof(appDbContext));
        _payPalService = payPalService ?? throw new ArgumentNullException(nameof(payPalService));
    }

    /// <inheritdoc />
    public async Task<Result> RegisterFreeAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken = default)
    {
        var competition = await _appDbContext.Competition.FirstOrDefaultAsync(c => c.CompetitionID == competitionId, cancellationToken);
        if (competition is null)
        {
            return Result.Fail(new NotFoundError("No such competition."));
        }

        if (!competition.OpenForRegistration)
        {
            return Result.Fail(new ConflictError("This competition is not open for registration."));
        }

        if (competition.EntranceFee > 0)
        {
            return Result.Fail(new ConflictError("This competition has an entrance fee - it cannot be joined for free."));
        }

        var conflict = await CheckNotAlreadyRegisteredAsync(userId, competitionId, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await CreateUserCompetitionAsync(userId, competitionId, amountPaid: 0, paymentProvider: null, paymentCreditId: null, cancellationToken);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> RegisterWithPaymentCreditAsync(Guid userId, Guid competitionId, string code, CancellationToken cancellationToken = default)
    {
        var competition = await _appDbContext.Competition.FirstOrDefaultAsync(c => c.CompetitionID == competitionId, cancellationToken);
        if (competition is null)
        {
            return Result.Fail(new NotFoundError("No such competition."));
        }

        if (!competition.OpenForRegistration)
        {
            return Result.Fail(new ConflictError("This competition is not open for registration."));
        }

        var credit = await _appDbContext.PaymentCredit
            .FirstOrDefaultAsync(p => p.UniquePaymentCode == code && p.ForCompetitionID == competitionId, cancellationToken);

        if (credit is null)
        {
            return Result.Fail(new PropertyValidationError(string.Empty, "This payment credit code is not valid for this competition."));
        }

        if (credit.CreditUsed)
        {
            return Result.Fail(new ConflictError("This payment credit code has already been used."));
        }

        var conflict = await CheckNotAlreadyRegisteredAsync(userId, competitionId, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        credit.CreditUsed = true;
        credit.UsedByUserID = userId;

        await CreateUserCompetitionAsync(userId, competitionId, amountPaid: 0, paymentProvider: null, paymentCreditId: credit.PaymentCreditID, cancellationToken);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<PayPalOrder>> CreatePayPalOrderAsync(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var competition = await _appDbContext.Competition.FirstOrDefaultAsync(c => c.CompetitionID == competitionId, cancellationToken);
        if (competition is null)
        {
            return Result.Fail<PayPalOrder>(new NotFoundError("No such competition."));
        }

        if (!competition.OpenForRegistration)
        {
            return Result.Fail<PayPalOrder>(new ConflictError("This competition is not open for registration."));
        }

        if (!competition.PayPalPaymentAvailable)
        {
            return Result.Fail<PayPalOrder>(new ConflictError("PayPal payment is not available for this competition."));
        }

        if (competition.EntranceFee <= 0)
        {
            return Result.Fail<PayPalOrder>(new ConflictError("This competition has no entrance fee to pay."));
        }

        var order = await _payPalService.CreateOrderAsync(competition.EntranceFee, cancellationToken);

        return Result.Ok(order);
    }

    /// <inheritdoc />
    public async Task<Result> CapturePayPalOrderAsync(Guid userId, Guid competitionId, string orderId, CancellationToken cancellationToken = default)
    {
        var competition = await _appDbContext.Competition.FirstOrDefaultAsync(c => c.CompetitionID == competitionId, cancellationToken);
        if (competition is null)
        {
            return Result.Fail(new NotFoundError("No such competition."));
        }

        var conflict = await CheckNotAlreadyRegisteredAsync(userId, competitionId, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        PayPalCapture capture;
        try
        {
            capture = await _payPalService.CaptureOrderAsync(orderId, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // PayPal rejects the capture call itself (not just a non-COMPLETED status) when the
            // order was never approved, was already captured, or the buyer declined - all
            // legitimate outcomes a user can hit, not a server error.
            return Result.Fail(new ConflictError("PayPal did not confirm this payment as complete."));
        }

        if (!string.Equals(capture.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Fail(new ConflictError("PayPal did not confirm this payment as complete."));
        }

        // Defensive - the amount is set from the competition's own entrance fee when the order is
        // created, so this should always match, but never treat a capture as valid without
        // checking what was actually charged against what should have been.
        if (capture.AmountCaptured != competition.EntranceFee)
        {
            return Result.Fail(new ConflictError("The captured payment amount did not match this competition's entrance fee."));
        }

        var userCompetition = await CreateUserCompetitionAsync(userId, competitionId, capture.AmountCaptured, "PayPal", paymentCreditId: null, cancellationToken);

        _appDbContext.Transaction.Add(new Transaction
        {
            TransactionID = Guid.NewGuid(),
            CompetitionID = competitionId,
            UserID = userId,
            UserCompetitionID = userCompetition.UserCompetitionID,
            Amount = competition.EntranceFee,
            ActualPaymentAmount = capture.AmountCaptured,
            TransactionStatus = capture.Status,
            PayPalTransactionID = capture.CaptureId,
            TransactionDateTime = DateTime.UtcNow,
            Failed = false
        });
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private async Task<Result?> CheckNotAlreadyRegisteredAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        var alreadyRegistered = await _appDbContext.UserCompetition
            .AnyAsync(uc => uc.UserID == userId && uc.CompetitionID == competitionId, cancellationToken);

        return alreadyRegistered ? Result.Fail(new ConflictError("You are already registered for this competition.")) : null;
    }

    private async Task<UserCompetition> CreateUserCompetitionAsync(Guid userId, Guid competitionId, decimal amountPaid, string? paymentProvider, Guid? paymentCreditId, CancellationToken cancellationToken)
    {
        // A newly joined competition always becomes the user's default - matches legacy behaviour
        // (both UserRegistration.aspx and UserCompetitionRegistration.aspx set IsDefaultCompetition
        // unconditionally on the row they create).
        var existingRegistrations = await _appDbContext.UserCompetition
            .Where(uc => uc.UserID == userId)
            .ToListAsync(cancellationToken);

        foreach (var registration in existingRegistrations)
        {
            registration.IsDefaultCompetition = false;
        }

        var userCompetition = new UserCompetition
        {
            UserCompetitionID = Guid.NewGuid(),
            UserID = userId,
            CompetitionID = competitionId,
            AmountPaid = amountPaid,
            PaymentProvider = paymentProvider,
            PaymentCreditID = paymentCreditId,
            IsDefaultCompetition = true
        };

        _appDbContext.UpdateRange(existingRegistrations);
        _appDbContext.UserCompetition.Add(userCompetition);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return userCompetition;
    }
}
