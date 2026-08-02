using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Constants;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize(Roles = RoleConstants.UserAdministrator)]
public class AnnouncementController : ApiControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementController(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    /// <summary>
    /// Get every non-expired announcement, most recent first, for the homepage and login-page feeds.
    /// </summary>
    [HttpGet("Active")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ActiveAnnouncementModel>>> GetActive(CancellationToken cancellationToken)
    {
        return Ok(await _announcementService.GetActiveAsync(cancellationToken));
    }

    /// <summary>
    /// Get every announcement including expired ones, most recent first, for the Announcements Admin page.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AnnouncementModel>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _announcementService.GetAllAsync(cancellationToken));
    }

    /// <summary>
    /// Get an announcement by its ID.
    /// </summary>
    /// <param name="id">Primary key of the announcement.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AnnouncementModel?>> Get(int id, CancellationToken cancellationToken)
    {
        var model = await _announcementService.GetById(id, cancellationToken);

        return OkOrNotFound(model);
    }

    /// <summary>
    /// Create a new announcement, recording the current user as its author.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    public async Task<ActionResult<AnnouncementModel?>> Post(CreateAnnouncementModel model, CancellationToken cancellationToken)
    {
        var result = await _announcementService.CreateAsync(model, CurrentUserId, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Edit an announcement.
    /// </summary>
    /// <param name="id">Primary key of the announcement to update, taken from the route.</param>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AnnouncementModel?>> Put(int id, AnnouncementModel model, CancellationToken cancellationToken)
    {
        var result = await _announcementService.Update(id, model, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Delete an announcement.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _announcementService.DeleteById(id, cancellationToken);

        return FromResult(result);
    }
}
