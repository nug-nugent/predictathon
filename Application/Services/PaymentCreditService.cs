using FluentResults;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Common;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;
using System.Security.Cryptography;

namespace Predictathon.Application.Services;

[ScopedService]
public class PaymentCreditService : IPaymentCreditService
{
    // Excludes visually-ambiguous characters (0/O, 1/I) - these codes are read aloud and typed by
    // hand by the recipient, unlike e.g. RefreshTokenService's tokens which are only ever handled
    // by code.
    private const string CodeCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 10;

    private readonly IApplicationDbContext _appDbContext;
    private readonly IValidator<CreatePaymentCreditModel>? _createValidator;

    public PaymentCreditService(IApplicationDbContext appDbContext, IValidator<CreatePaymentCreditModel>? createValidator = null)
    {
        _appDbContext = appDbContext ?? throw new ArgumentNullException(nameof(appDbContext));
        _createValidator = createValidator;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentCreditListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _appDbContext.PaymentCredit
            .OrderByDescending(p => p.IssueDate)
            .ThenBy(p => p.ExpectedUsername)
            .Select(p => new PaymentCreditListItem
            {
                PaymentCreditID = p.PaymentCreditID,
                ExpectedUsername = p.ExpectedUsername,
                CompetitionID = p.ForCompetitionID,
                CompetitionName = p.ForCompetition.CompetitionName,
                UniquePaymentCode = p.UniquePaymentCode,
                CreditUsed = p.CreditUsed,
                IssueDate = p.IssueDate,
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<PaymentCreditListItem>> CreateAsync(CreatePaymentCreditModel model, Guid issuedByUserId, CancellationToken cancellationToken = default)
    {
        if (_createValidator is not null)
        {
            var validation = await _createValidator.ValidateAsync(model, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new PropertyValidationError(e.PropertyName, e.ErrorMessage)).ToArray();
                return Result.Fail<PaymentCreditListItem>(errors);
            }
        }

        var competition = await _appDbContext.Competition.FirstOrDefaultAsync(c => c.CompetitionID == model.CompetitionID, cancellationToken);
        if (competition is null)
        {
            return Result.Fail<PaymentCreditListItem>(new NotFoundError("No such competition."));
        }

        var credit = new PaymentCredit
        {
            PaymentCreditID = Guid.NewGuid(),
            ForCompetitionID = model.CompetitionID,
            ExpectedUsername = model.ExpectedUsername.Trim(),
            UniquePaymentCode = GenerateCode(),
            IssuedByUserID = issuedByUserId,
            IssueDate = UkClock.Now,
        };

        _appDbContext.PaymentCredit.Add(credit);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new PaymentCreditListItem
        {
            PaymentCreditID = credit.PaymentCreditID,
            ExpectedUsername = credit.ExpectedUsername,
            CompetitionID = credit.ForCompetitionID,
            CompetitionName = competition.CompetitionName,
            UniquePaymentCode = credit.UniquePaymentCode,
            CreditUsed = credit.CreditUsed,
            IssueDate = credit.IssueDate,
        });
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid paymentCreditId, CancellationToken cancellationToken = default)
    {
        var credit = await _appDbContext.PaymentCredit.FirstOrDefaultAsync(p => p.PaymentCreditID == paymentCreditId, cancellationToken);
        if (credit is null)
        {
            return Result.Fail(new NotFoundError("No such payment credit."));
        }

        _appDbContext.PaymentCredit.Remove(credit);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private static string GenerateCode()
    {
        var chars = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            chars[i] = CodeCharacters[RandomNumberGenerator.GetInt32(CodeCharacters.Length)];
        }

        return new string(chars);
    }
}
