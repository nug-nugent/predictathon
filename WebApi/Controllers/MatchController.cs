using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Constants;
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

    /// <summary>
    /// Get the earliest not-yet-predicted match at least 5 minutes from now for the current user in
    /// a competition, or null if every future match has already been predicted.
    /// </summary>
    [HttpGet("{competitionId:guid}/NextUnpredicted")]
    public async Task<ActionResult<UserMatchPredictionListItem?>> GetNextUnpredicted(Guid competitionId, CancellationToken cancellationToken)
    {
        var match = await _matchService.GetNextUnpredictedMatchAsync(CurrentUserId, competitionId, cancellationToken);

        return Ok(match);
    }

    /// <summary>
    /// Get every match for a competition for admin management.
    /// </summary>
    /// <param name="competitionId"></param>
    /// <param name="includePlayed">Whether to include already-played matches. Defaults to false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("Admin/{competitionId:guid}")]
    [Authorize(Roles = RoleConstants.MatchAdministrator)]
    public async Task<ActionResult<IReadOnlyList<MatchModel>>> GetForAdmin(
        Guid competitionId,
        [FromQuery] bool includePlayed,
        CancellationToken cancellationToken)
    {
        var matches = await _matchService.GetForAdminAsync(competitionId, includePlayed, cancellationToken);

        return Ok(matches);
    }

    /// <summary>
    /// Create a new match.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleConstants.MatchAdministrator)]
    public async Task<ActionResult<MatchModel?>> Post(CreateMatchModel model, CancellationToken cancellationToken)
    {
        var result = await _matchService.Create(model, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Edit a match.
    /// </summary>
    /// <param name="id">Primary key of the match to update, taken from the route.</param>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleConstants.MatchAdministrator)]
    public async Task<ActionResult<MatchModel?>> Put(Guid id, MatchModel model, CancellationToken cancellationToken)
    {
        if (model.MatchID != Guid.Empty && model.MatchID != id)
        {
            return BadRequestProblem(
                detail: "The match id in the route does not match the MatchID in the request body.",
                title: "ID mismatch");
        }

        var result = await _matchService.Update(id, model, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Delete a match.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleConstants.MatchAdministrator)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _matchService.DeleteById(id, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Get unplayed matches for a competition whose kickoff has already passed - the pool of
    /// matches a result could plausibly be entered for.
    /// </summary>
    [HttpGet("Processing/{competitionId:guid}")]
    [Authorize(Roles = RoleConstants.MatchAdministrator)]
    public async Task<ActionResult<IReadOnlyList<MatchModel>>> GetForProcessing(Guid competitionId, CancellationToken cancellationToken)
    {
        var matches = await _matchService.GetForProcessingAsync(competitionId, cancellationToken);

        return Ok(matches);
    }

    /// <summary>
    /// Record a match's final score and mark it played.
    /// </summary>
    [HttpPost("Result")]
    [Authorize(Roles = RoleConstants.MatchAdministrator)]
    public async Task<ActionResult<MatchModel?>> SaveResult(SaveMatchResultRequest request, CancellationToken cancellationToken)
    {
        var result = await _matchService.SaveResultAsync(request.MatchID, request.HomeTeamGoals, request.AwayTeamGoals, cancellationToken);

        return FromResult(result);
    }
}
