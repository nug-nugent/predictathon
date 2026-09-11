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
    private readonly ILiveScoreService _liveScoreService;
    private readonly IBracketResolutionService _bracketResolutionService;

    public MatchController(
        IMatchService matchService,
        ILiveScoreService liveScoreService,
        IBracketResolutionService bracketResolutionService)
    {
        _matchService = matchService;
        _liveScoreService = liveScoreService;
        _bracketResolutionService = bracketResolutionService;
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
    /// Get a competition's knockout bracket - its rounds in order plus the third-place play-off,
    /// each match joined with the current user's own prediction for it. Comes back with no rounds
    /// for a competition that has no bracket.
    /// </summary>
    /// <param name="competitionId">The competition whose bracket is wanted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{competitionId:guid}/Bracket")]
    public async Task<ActionResult<KnockoutBracketModel>> GetKnockoutBracket(Guid competitionId, CancellationToken cancellationToken)
    {
        var bracket = await _matchService.GetKnockoutBracketAsync(CurrentUserId, competitionId, cancellationToken);

        return Ok(bracket);
    }

    /// <summary>
    /// Every slot in a competition's bracket, with what it is waiting on and the team its
    /// placeholder now resolves to. Read-only - see <see cref="ResolveBracket"/> to act on it.
    /// </summary>
    /// <param name="competitionId">The competition whose bracket is wanted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{competitionId:guid}/Bracket/Resolution")]
    [Authorize(Roles = RoleConstants.MatchAdministrator)]
    public async Task<ActionResult<BracketResolutionModel>> GetBracketResolution(Guid competitionId, CancellationToken cancellationToken)
    {
        var resolution = await _bracketResolutionService.GetAsync(competitionId, cancellationToken);

        return Ok(resolution);
    }

    /// <summary>
    /// Fill in the bracket slots an admin has picked, from the proposals in
    /// <see cref="GetBracketResolution"/>. Slots that already hold a team are left alone.
    /// </summary>
    /// <param name="competitionId">The competition whose bracket is being filled in.</param>
    /// <param name="assignments">The slots to fill, and the team for each.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{competitionId:guid}/Bracket/Resolution")]
    [Authorize(Roles = RoleConstants.MatchAdministrator)]
    public async Task<ActionResult<BracketResolutionSummary>> ResolveBracket(
        Guid competitionId,
        [FromBody] IReadOnlyList<BracketSlotAssignment> assignments,
        CancellationToken cancellationToken)
    {
        var result = await _bracketResolutionService.ApplyAsync(competitionId, assignments, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Number a competition's bracket slots from its kick-off times, round by round, replacing any
    /// already set. A starting point for numbering a draw by hand, not a substitute for it - see
    /// IMatchService.NumberBracketByKickOffAsync.
    /// </summary>
    /// <param name="competitionId">The competition whose bracket should be numbered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{competitionId:guid}/NumberBracket")]
    [Authorize(Roles = RoleConstants.CompetitionAdministrator)]
    public async Task<ActionResult<BracketNumberingSummary>> NumberBracket(Guid competitionId, CancellationToken cancellationToken)
    {
        var summary = await _matchService.NumberBracketByKickOffAsync(competitionId, cancellationToken);

        return Ok(summary);
    }

    /// <summary>
    /// Get a user's prediction history for a competition, most recent first. Future matches are
    /// only included when the caller is viewing their own history.
    /// </summary>
    [HttpGet("{competitionId:guid}/UserPredictions/{userId:guid}")]
    public async Task<ActionResult<IReadOnlyList<UserMatchPredictionListItem>>> GetUserPredictionHistory(Guid competitionId, Guid userId, CancellationToken cancellationToken)
    {
        var includeFuture = userId == CurrentUserId;
        var matches = await _matchService.GetUserPredictionHistoryAsync(userId, competitionId, includeFuture, cancellationToken);

        return Ok(matches);
    }

    /// <summary>
    /// Get today's matches for a competition, each joined with the current user's own prediction
    /// for it (if any) - the Home page's Today's Matches section and the Live page.
    /// </summary>
    /// <param name="competitionId"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{competitionId:guid}/Today")]
    public async Task<ActionResult<IReadOnlyList<UserMatchPredictionListItem>>> GetToday(Guid competitionId, CancellationToken cancellationToken)
    {
        var matches = await _matchService.GetLiveDayMatchesAsync(CurrentUserId, competitionId, cancellationToken);

        return Ok(matches);
    }

    /// <summary>
    /// Get every played match for a competition, most recent first, for the public Results page.
    /// </summary>
    [HttpGet("{competitionId:guid}/Results")]
    public async Task<ActionResult<IReadOnlyList<MatchListItem>>> GetResults(Guid competitionId, CancellationToken cancellationToken)
    {
        var results = await _matchService.GetResultsAsync(competitionId, CurrentUserId, cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Get a single played match's result and prediction stats, for the Match Detail page.
    /// </summary>
    [HttpGet("{competitionId:guid}/{matchId:guid}/Detail")]
    public async Task<ActionResult<MatchListItem?>> GetDetail(Guid competitionId, Guid matchId, CancellationToken cancellationToken)
    {
        var detail = await _matchService.GetMatchDetailAsync(competitionId, matchId, CurrentUserId, cancellationToken);

        return OkOrNotFound(detail);
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
    /// Record or correct a match's provisional in-play score, shown on the Live page while the
    /// match is being played. Separate from SaveResult: this score is provisional and scores no
    /// predictions.
    /// </summary>
    /// <param name="matchId">The match to score, taken from the route.</param>
    /// <param name="request"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{matchId:guid}/LiveScore")]
    [Authorize(Roles = RoleConstants.MatchAdministrator)]
    public async Task<ActionResult<MatchLiveScoreModel?>> SaveLiveScore(
        Guid matchId,
        SaveLiveScoreRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _liveScoreService.SaveAdminScoreAsync(
            matchId, request.HomeTeamGoals, request.AwayTeamGoals, CurrentUserId, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Get unplayed matches for a competition whose kickoff was at least 90 minutes ago - the pool
    /// of matches a result can actually be entered for.
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
        var result = await _matchService.SaveResultAsync(
            request.MatchID, request.HomeTeamGoals, request.AwayTeamGoals, CurrentUserId, cancellationToken);

        return FromResult(result);
    }
}
