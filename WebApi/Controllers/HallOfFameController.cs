using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
