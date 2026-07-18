using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

/// <summary>
/// Endpoints for scheduled maintenance tasks (daily prediction reminder emails, league-history
/// snapshots) - replaces the legacy UI/Web/Pages/Tasks/ScheduledTasks.aspx. Deliberately
/// unauthenticated, matching legacy: in production each of these is one URL configured as an
/// UptimeRobot HTTP(s) monitor, so a routine "is it up" heartbeat ping doubles as the scheduler -
/// no real cron infrastructure needed. GET, not POST, since that's the HTTP method a URL-based
/// uptime monitor uses.
/// </summary>
[AllowAnonymous]
public class TasksController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly ICompetitionService _competitionService;

    public TasksController(IUserService userService, ICompetitionService competitionService)
    {
        _userService = userService;
        _competitionService = competitionService;
    }

    /// <summary>
    /// Emails every user with a prediction due within their reminder window. Matches legacy's
    /// ScheduledTask.PredictionEmailReminderSend.
    /// </summary>
    [HttpGet("PredictionEmailReminderSend")]
    public async Task<ActionResult> PredictionEmailReminderSend(CancellationToken cancellationToken)
    {
        await _userService.SendPredictionEmailRemindersAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Snapshots today's league position for every user in every competition. Matches legacy's
    /// ScheduledTask.UserCompetitionLeagueHistorySet.
    /// </summary>
    [HttpGet("UserCompetitionLeagueHistorySet")]
    public async Task<ActionResult> UserCompetitionLeagueHistorySet()
    {
        await _competitionService.SetUserCompetitionLeagueHistoryAsync();

        return NoContent();
    }
}
