using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Constants;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

public class CompetitionController : ApiControllerBase
{
    private readonly ICompetitionService _competitionService;
    private readonly ILogger<CompetitionController> _logger;

    public CompetitionController(
        ICompetitionService competitionService,
        ILogger<CompetitionController> logger
    )
    {
        _competitionService = competitionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all competitions, most recently started first.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompetitionModel>>> GetAll()
    {
        var competitions = await _competitionService.GetCompetitionListAsync();

        return Ok(competitions.Adapt<List<CompetitionModel>>());
    }

    /// <summary>
    /// Get the competitions the current user is registered for, most recently started first.
    /// </summary>
    [HttpGet("MyRegistrations")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<UserCompetitionRegistrationListItem>>> GetMyRegistrations()
    {
        var registrations = await _competitionService.GetUserCompetitionRegistrationListAsync(CurrentUserId);

        return Ok(registrations.Where(r => r.Registered).ToList());
    }

    /// <summary>
    /// Get a competition by its ID.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompetitionModel?>> Get(Guid id, CancellationToken cancellationToken)
    {
        var model = await _competitionService.GetById(id, cancellationToken);

        return OkOrNotFound(model);
    }

    /// <summary>
    /// Get the Friday-starting weeks a competition has matches in, earliest first.
    /// </summary>
    [HttpGet("{id:guid}/Weeks")]
    public async Task<ActionResult<IReadOnlyList<DateTime>>> GetWeeks(Guid id)
    {
        return Ok(await _competitionService.GetCompetitionWeeksAsync(id));
    }

    /// <summary>
    /// Create a new competition.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Roles = RoleConstants.CompetitionAdministrator)]
    public async Task<ActionResult<CompetitionModel?>> Post(CompetitionModel model, CancellationToken cancellationToken)
    {
        var result = await _competitionService.Create(model, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Edit a competition.
    /// </summary>
    /// <param name="id">Primary key of the competition to update, taken from the route.</param>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleConstants.CompetitionAdministrator)]
    public async Task<ActionResult<CompetitionModel?>> Put(Guid id, CompetitionModel model, CancellationToken cancellationToken)
    {
        if (model.CompetitionID != Guid.Empty && model.CompetitionID != id)
        {
            return BadRequestProblem(
                detail: "The competition id in the route does not match the CompetitionID in the request body.",
                title: "ID mismatch");
        }

        var result = await _competitionService.Update(id, model, cancellationToken);

        // Convert service Result<T> into ActionResult with consistent ProblemDetails on validation failures.
        return FromResult(result);
    }

    /// <summary>
    /// Delete a competition.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleConstants.CompetitionAdministrator)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _competitionService.DeleteById(id, cancellationToken);

        // Convert service Result into ActionResult with consistent ProblemDetails on failure.
        return FromResult(result);
    }
}
