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
    /// Get the all-time league table - one row per user across every competition they've ever
    /// been registered for, ranked the same way as a single competition's league table.
    /// </summary>
    [HttpGet("AllTimeLeagueTable")]
    public async Task<ActionResult<IReadOnlyList<LeagueTableItem>>> GetAllTimeLeagueTable(CancellationToken cancellationToken)
    {
        return Ok(await _statisticsService.GetAllTimeLeagueTableAsync(cancellationToken));
    }

    /// <summary>
    /// Get every statistics widget scoped to a specific competition, personalised to the current user.
    /// </summary>
    [HttpGet("CurrentCompetition/{competitionId:guid}")]
    public async Task<ActionResult<CurrentCompetitionStatisticsModel>> GetCurrentCompetition(Guid competitionId, CancellationToken cancellationToken)
    {
        return Ok(await _statisticsService.GetCurrentCompetitionStatisticsAsync(competitionId, CurrentUserId, cancellationToken));
    }

    /// <summary>
    /// Get the predictions that beat the average prediction score for their match, best first,
    /// optionally restricted to matches within a date range.
    /// </summary>
    /// <param name="competitionId">The competition to search within.</param>
    /// <param name="dateFrom">Only include matches played on or after this date.</param>
    /// <param name="dateTo">Only include matches played on or before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("CurrentCompetition/{competitionId:guid}/BestPredictions")]
    public async Task<ActionResult<IReadOnlyList<BestPredictionListItem>>> GetBestPredictions(
        Guid competitionId, [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken cancellationToken)
    {
        return Ok(await _statisticsService.GetBestPredictionsAsync(competitionId, dateFrom, dateTo, cancellationToken));
    }
}
