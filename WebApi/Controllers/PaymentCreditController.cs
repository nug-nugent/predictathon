using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Constants;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize(Roles = RoleConstants.UserAdministrator)]
public class PaymentCreditController : ApiControllerBase
{
    private readonly IPaymentCreditService _paymentCreditService;

    public PaymentCreditController(IPaymentCreditService paymentCreditService)
    {
        _paymentCreditService = paymentCreditService;
    }

    /// <summary>
    /// Get every payment credit across all competitions, most recently issued first, for the
    /// Payment Credit Admin page.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentCreditListItem>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _paymentCreditService.GetAllAsync(cancellationToken));
    }

    /// <summary>
    /// Issue a new payment credit code for a competition.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PaymentCreditListItem?>> Post(CreatePaymentCreditModel model, CancellationToken cancellationToken)
    {
        var result = await _paymentCreditService.CreateAsync(model, CurrentUserId, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Delete a payment credit.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _paymentCreditService.DeleteAsync(id, cancellationToken);

        return FromResult(result);
    }
}
