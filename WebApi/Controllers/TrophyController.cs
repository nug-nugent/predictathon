using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

/// <summary>
/// Trophies for surfaces that have no payload of their own to carry them - the profile page and
/// the message board get theirs folded into the models they already fetch, so this exists for the
/// likes of the Home dashboard's own-profile card.
/// </summary>
[Authorize]
public class TrophyController : ApiControllerBase
{
    private readonly ITrophyService _trophyService;

    public TrophyController(ITrophyService trophyService)
    {
        _trophyService = trophyService;
    }

    /// <summary>
    /// Get a user's trophies, best-known series first. Empty for anyone who has never won a
    /// competition, which is most people.
    /// </summary>
    /// <param name="userId">The user whose trophies to get.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("User/{userId:guid}")]
    public async Task<ActionResult<IReadOnlyList<UserTrophyModel>>> GetForUser(Guid userId, CancellationToken cancellationToken)
    {
        return Ok(await _trophyService.GetForUserAsync(userId, cancellationToken));
    }
}
