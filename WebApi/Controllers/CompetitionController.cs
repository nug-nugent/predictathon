using Microsoft.AspNetCore.Mvc;
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

    // TODO - Add a POST endpoint to create a new competition (solid validation foundation)
    // TODO - Add a Competitions/GET endpoint to retrieve all competitions (with pagination and filtering)

    /// <summary>
    /// Get a competition by its ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompetitionModel?>> Get(Guid id)
    {
        var model = await _competitionService.GetById(id);

        return OkOrNotFound(model);
    }

    public async Task<ActionResult<CompetitionModel?>> Post(CompetitionModel model)
    {
        var result = await _competitionService.Create(model);

        return FromResult(result);
    }

    /// <summary>
    /// Edit a competition.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CompetitionModel?>> Put(CompetitionModel model)
    {
        var result = await _competitionService.Update(model.CompetitionID, model);

        // Convert service Result<T> into ActionResult with consistent ProblemDetails on validation failures.
        return FromResult(result);
    }

    /// <summary>
    /// Delete a competition.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _competitionService.DeleteById(id);

        // Convert service Result<T> into ActionResult with consistent ProblemDetails on validation failures.
        return new JsonResult(result.IsSuccess);
    }
}
