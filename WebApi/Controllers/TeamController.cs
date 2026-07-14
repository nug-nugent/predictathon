using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize]
public class TeamController : ApiControllerBase
{
    private readonly ITeamService _teamService;

    public TeamController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    /// <summary>
    /// Get the teams registered for a competition, ordered by name.
    /// </summary>
    [HttpGet("{competitionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<TeamModel>>> GetForCompetition(Guid competitionId, CancellationToken cancellationToken)
    {
        var teams = await _teamService.GetForCompetitionAsync(competitionId, cancellationToken);

        return Ok(teams);
    }
}
