using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers
{
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
        // TODO - Add a PUT endpoint to update an existing competition
        // TODO - Add a DELETE endpoint to delete an existing competition?
        // TODO - Add a Competitions/GET endpoint to retrieve all competitions (with pagination and filtering)

        //[HttpGet(Name = "Get")]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CompetitionModel?>> Get(Guid id)
        {
            var model = await _competitionService.GetById(id);

            return OkOrNotFound(model);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CompetitionModel?>> Put(Guid id, CompetitionModel model)
        {
            var result = await _competitionService.Update(id, model);

            // Convert service Result<T> into ActionResult with consistent ProblemDetails on validation failures.
            return FromResult(result);
        }
    }
}
