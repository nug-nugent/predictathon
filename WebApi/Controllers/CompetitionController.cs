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

    // TODO - Add a Competitions/GET endpoint to retrieve all competitions (with pagination and filtering)

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
    /// Create a new competition.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPost]
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
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _competitionService.DeleteById(id, cancellationToken);

        // Convert service Result into ActionResult with consistent ProblemDetails on failure.
        return FromResult(result);
    }
}
