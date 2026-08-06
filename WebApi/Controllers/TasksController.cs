using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Predictathon.Application.Interfaces;
using Predictathon.WebApi.Controllers.Base;
using Predictathon.WebApi.Options;

namespace Predictathon.WebApi.Controllers;

/// <summary>
/// Endpoints for scheduled maintenance tasks (daily prediction reminder emails, league-history
/// snapshots), meant to be triggered by an external scheduler - an UptimeRobot monitor or a Plesk
/// scheduled task - rather than an in-process timer. Shared hosting app pools recycle on idle and
/// have no "Always On" option, so nothing inside the process can be trusted to run on a schedule
/// reliably. GET, not POST, since that's the HTTP method a URL-based monitor uses. Both operations
/// are idempotent (safe to call more than once for the same day), so the API key below is a
/// convenience check rather than a hard security boundary.
/// </summary>
[AllowAnonymous]
public class TasksController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly ICompetitionService _competitionService;
    private readonly string? _apiKey;

    /// <summary>
    /// Initialises a new instance of the <see cref="TasksController"/> class.
    /// </summary>
    /// <param name="userService">Used to send prediction reminder emails.</param>
    /// <param name="competitionService">Used to snapshot league-history.</param>
    /// <param name="options">Supplies the configured shared-secret API key, if any.</param>
    public TasksController(IUserService userService, ICompetitionService competitionService, IOptions<ScheduledTasksOptions> options)
    {
        _userService = userService;
        _competitionService = competitionService;
        _apiKey = options.Value.ApiKey;
    }

    /// <summary>
    /// Emails every user with a prediction due within their reminder window. Matches legacy's
    /// ScheduledTask.PredictionEmailReminderSend.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("PredictionEmailReminderSend")]
    public async Task<ActionResult> PredictionEmailReminderSend(CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

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
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        await _competitionService.SetUserCompetitionLeagueHistoryAsync();

        return NoContent();
    }

    /// <summary>
    /// Checks the "X-Api-Key" request header against the configured key. Always authorized when
    /// no key is configured, matching <see cref="Predictathon.WebApi.HealthChecks.ApiKeyEndpointFilter"/>'s
    /// behaviour for the health-check endpoints.
    /// </summary>
    private bool IsAuthorized()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return true;
        }

        return string.Equals(Request.Headers["X-Api-Key"].ToString(), _apiKey, StringComparison.Ordinal);
    }
}
