using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;
using Predictathon.WebApi.Models;

namespace Predictathon.WebApi.Controllers;

[Authorize]
public class UserController : ApiControllerBase
{
    // A generous cap on the ORIGINAL upload (before any resize) - well above what a phone photo
    // needs, but low enough to bound memory/decode cost per request.
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private readonly IUserService _userService;
    private readonly IAvatarService _avatarService;

    public UserController(IUserService userService, IAvatarService avatarService)
    {
        _userService = userService;
        _avatarService = avatarService;
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

    /// <summary>
    /// Uploads the current user's avatar. Always acts on the caller's own account (from the JWT) -
    /// there's no target-user parameter, so there's nothing for a client to spoof to affect
    /// someone else's avatar.
    /// </summary>
    [HttpPost("Avatar")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult> UploadAvatar([FromForm] UploadAvatarRequest request, CancellationToken cancellationToken)
    {
        if (request.Image.Length == 0)
        {
            return BadRequestProblem(detail: "No file was uploaded.");
        }

        if (request.Image.Length > MaxUploadBytes)
        {
            return BadRequestProblem(detail: "The uploaded file is too large.");
        }

        var crop = new AvatarCropRect
        {
            X = request.CropX,
            Y = request.CropY,
            Width = request.CropWidth,
            Height = request.CropHeight,
        };

        await using var stream = request.Image.OpenReadStream();
        var result = await _avatarService.UploadAvatarAsync(CurrentUserId, stream, crop, cancellationToken);

        if (result.IsFailed)
        {
            return FromResult(result);
        }

        return Ok(new { avatarUrl = _avatarService.GetAvatarUrl(CurrentUserId, imageUploaded: true) });
    }

    /// <summary>
    /// Removes the current user's avatar, reverting to the initials fallback.
    /// </summary>
    [HttpDelete("Avatar")]
    public async Task<ActionResult> RemoveAvatar(CancellationToken cancellationToken)
    {
        await _avatarService.RemoveAvatarAsync(CurrentUserId, cancellationToken);

        return NoContent();
    }
}
