using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;

namespace Predictathon.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CompetitionController : ControllerBase
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

        [HttpGet(Name = "Get")]
        public async Task<CompetitionModel?> Get()
        {
            return await _competitionService.GetById(new Guid("38893FFB-7EF3-4F27-8766-0B32FDF8F2EF"));
        }

        //[HttpGet(Name = "List")]
        //public IEnumerable<CompetitionModel> GetList()
        //{
        //    return _competitionService.GetList();
        //}
    }
}
