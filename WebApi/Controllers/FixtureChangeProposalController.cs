using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Constants;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize(Roles = RoleConstants.MatchAdministrator)]
public class FixtureChangeProposalController : ApiControllerBase
{
    private readonly IFixtureChangeProposalService _fixtureChangeProposalService;

    public FixtureChangeProposalController(IFixtureChangeProposalService fixtureChangeProposalService)
    {
        _fixtureChangeProposalService = fixtureChangeProposalService;
    }

    /// <summary>
    /// Get every pending fixture-change proposal, earliest proposed kickoff first.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FixtureChangeProposalModel>>> GetPending(CancellationToken cancellationToken)
    {
        var proposals = await _fixtureChangeProposalService.GetPendingAsync(cancellationToken);

        return Ok(proposals);
    }

    /// <summary>
    /// Run a fixture-change detection pass immediately. This is the only way detection runs -
    /// there's no background schedule, by design, so it only ever costs an external API call when
    /// an admin actually asks for one.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("SyncNow")]
    public async Task<ActionResult> SyncNow(CancellationToken cancellationToken)
    {
        var result = await _fixtureChangeProposalService.DetectChangesAsync(cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Apply a pending proposal's new kickoff time to its match and mark the proposal Confirmed.
    /// </summary>
    /// <param name="id">Primary key of the proposal to confirm.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id:int}/Confirm")]
    public async Task<ActionResult> Confirm(int id, CancellationToken cancellationToken)
    {
        var result = await _fixtureChangeProposalService.ConfirmAsync(id, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Mark a pending proposal Dismissed without changing its match.
    /// </summary>
    /// <param name="id">Primary key of the proposal to dismiss.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id:int}/Dismiss")]
    public async Task<ActionResult> Dismiss(int id, CancellationToken cancellationToken)
    {
        var result = await _fixtureChangeProposalService.DismissAsync(id, cancellationToken);

        return FromResult(result);
    }
}
