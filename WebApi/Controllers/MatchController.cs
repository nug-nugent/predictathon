using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize]
public class MatchController : ApiControllerBase
{
    private readonly IMatchService _matchService;

    public MatchController(IMatchService matchService)
    {
        _matchService = matchService;
    }

    /// <summary>
    /// Get the matches in the 7-day week starting at <paramref name="dateFrom"/> for a competition,
    /// each joined with the current user's own prediction for it (if any).
    /// </summary>
    /// <param name="competitionId"></param>
    /// <param name="dateFrom">The first day of the week to get matches for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{competitionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<UserMatchPredictionListItem>>> GetForWeek(
        Guid competitionId,
        [FromQuery] DateTime dateFrom,
        CancellationToken cancellationToken)
    {
        var matches = await _matchService.GetUserMatchesForWeekAsync(CurrentUserId, competitionId, dateFrom, cancellationToken);

        return Ok(matches);
    }
}
