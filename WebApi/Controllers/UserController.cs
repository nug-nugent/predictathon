using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize]
public class UserController : ApiControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get a user's publicly-viewable profile.
    /// </summary>
    [HttpGet("{userId:guid}/Profile")]
    public async Task<ActionResult<UserProfileModel?>> GetProfile(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _userService.GetProfileAsync(userId, cancellationToken);

        return OkOrNotFound(profile);
    }
}
