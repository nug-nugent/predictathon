using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize]
public class LeagueController : ApiControllerBase
{
    private readonly ILeagueTableService _leagueTableService;

    public LeagueController(ILeagueTableService leagueTableService)
    {
        _leagueTableService = leagueTableService;
    }

    /// <summary>
    /// Get the league table for a competition.
    /// </summary>
    /// <param name="competitionId"></param>
    /// <param name="dateFrom">Only include matches played on or after this date.</param>
    /// <param name="dateTo">Only include matches played on or before this date.</param>
    /// <param name="dateForComparison">If supplied, each row's previous position as of this date is included.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpGet("{competitionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<LeagueTableItem>>> Get(
        Guid competitionId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] DateOnly? dateForComparison,
        CancellationToken cancellationToken)
    {
        var results = await _leagueTableService.GetLeagueTableAsync(competitionId, dateFrom, dateTo, dateForComparison, cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Get the league table for a competition, each row carrying the points the matches currently
    /// in play are worth to that user so far - the Live page's table.
    /// </summary>
    /// <param name="competitionId"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{competitionId:guid}/Live")]
    public async Task<ActionResult<IReadOnlyList<LiveLeagueTableItem>>> GetLive(Guid competitionId, CancellationToken cancellationToken)
    {
        var results = await _leagueTableService.GetLiveLeagueTableAsync(competitionId, cancellationToken);

        return Ok(results);
    }
}
