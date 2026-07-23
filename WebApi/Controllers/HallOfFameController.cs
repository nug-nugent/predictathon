using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Constants;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize]
public class HallOfFameController : ApiControllerBase
{
    private readonly IHallOfFameService _hallOfFameService;

    public HallOfFameController(IHallOfFameService hallOfFameService)
    {
        _hallOfFameService = hallOfFameService;
    }

    /// <summary>
    /// Get every Hall of Fame entry, most recently concluded competition first.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HallOfFameListItem>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _hallOfFameService.GetAllAsync(cancellationToken);

        return Ok(items);
    }

    /// <summary>
    /// Get whether a competition is currently eligible to have its Hall of Fame entry auto-generated.
    /// </summary>
    [HttpGet("{competitionId:guid}/GenerationStatus")]
    public async Task<ActionResult<HallOfFameGenerationStatus>> GetGenerationStatus(Guid competitionId, CancellationToken cancellationToken)
    {
        var status = await _hallOfFameService.GetGenerationStatusAsync(competitionId, cancellationToken);

        return Ok(status);
    }

    /// <summary>
    /// Generate a competition's Hall of Fame entry (1st/2nd/3rd place) from its live league table.
    /// </summary>
    [HttpPost("{competitionId:guid}/Generate")]
    [Authorize(Roles = RoleConstants.CompetitionAdministrator)]
    public async Task<ActionResult<HallOfFameListItem?>> Generate(Guid competitionId, CancellationToken cancellationToken)
    {
        var result = await _hallOfFameService.GenerateForCompetitionAsync(competitionId, cancellationToken);

        return FromResult(result);
    }
}
