using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize]
public class PredictionController : ApiControllerBase
{
    private readonly IPredictionService _predictionService;

    public PredictionController(IPredictionService predictionService)
    {
        _predictionService = predictionService;
    }

    /// <summary>
    /// Upsert the current user's prediction for a match.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    public async Task<ActionResult> Save(SavePredictionRequest request, CancellationToken cancellationToken)
    {
        var result = await _predictionService.SavePredictionAsync(
            request.MatchID, CurrentUserId, request.HomeTeamGoals, request.AwayTeamGoals, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Get every registered competitor's prediction for a match.
    /// </summary>
    /// <param name="matchId"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("Match/{matchId:guid}")]
    public async Task<ActionResult<IReadOnlyList<MatchPredictionListItem>?>> GetForMatch(Guid matchId, CancellationToken cancellationToken)
    {
        var result = await _predictionService.GetMatchPredictionsAsync(matchId, cancellationToken);

        return FromResult(result);
    }
}
