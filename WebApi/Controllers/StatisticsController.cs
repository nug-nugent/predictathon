using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize]
public class StatisticsController : ApiControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// Get every all-time (not competition-scoped) statistics widget.
    /// </summary>
    [HttpGet("AllTime")]
    public async Task<ActionResult<AllTimeStatisticsModel>> GetAllTime(CancellationToken cancellationToken)
    {
        return Ok(await _statisticsService.GetAllTimeStatisticsAsync(cancellationToken));
    }

    /// <summary>
    /// Get every statistics widget scoped to a specific competition, personalised to the current user.
    /// </summary>
    [HttpGet("CurrentCompetition/{competitionId:guid}")]
    public async Task<ActionResult<CurrentCompetitionStatisticsModel>> GetCurrentCompetition(Guid competitionId, CancellationToken cancellationToken)
    {
        return Ok(await _statisticsService.GetCurrentCompetitionStatisticsAsync(competitionId, CurrentUserId, cancellationToken));
    }
}
