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
    private readonly IUserCompetitionService _userCompetitionService;
    private readonly IFixtureImportService _fixtureImportService;
    private readonly ILogger<CompetitionController> _logger;

    public CompetitionController(
        ICompetitionService competitionService,
        IUserCompetitionService userCompetitionService,
        IFixtureImportService fixtureImportService,
        ILogger<CompetitionController> logger
    )
    {
        _competitionService = competitionService;
        _userCompetitionService = userCompetitionService;
        _fixtureImportService = fixtureImportService;
        _logger = logger;
    }

    /// <summary>
    /// Get the competitions currently open for registration, for the pre-login landing page.
    /// </summary>
    [HttpGet("OpenForRegistration")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CompetitionRegistrationSummaryModel>>> GetOpenForRegistration()
    {
        var competitions = await _competitionService.GetCompetitionListForLoginPageAsync();

        return Ok(competitions.Adapt<List<CompetitionRegistrationSummaryModel>>());
    }

    /// <summary>
    /// Get site-wide stats for the pre-login landing page.
    /// </summary>
    [HttpGet("PublicStats")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicStatsModel>> GetPublicStats(CancellationToken cancellationToken)
    {
        return Ok(await _competitionService.GetPublicStatsAsync(cancellationToken));
    }

    /// <summary>
    /// Register the current user for a competition that has no entrance fee.
    /// </summary>
    [HttpPost("{id:guid}/Register/Free")]
    [Authorize]
    public async Task<ActionResult> RegisterFree(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userCompetitionService.RegisterFreeAsync(CurrentUserId, id, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Register the current user for a competition by redeeming a payment credit code.
    /// </summary>
    [HttpPost("{id:guid}/Register/PaymentCredit")]
    [Authorize]
    public async Task<ActionResult> RegisterWithPaymentCredit(Guid id, RegisterWithPaymentCreditModel model, CancellationToken cancellationToken)
    {
        var result = await _userCompetitionService.RegisterWithPaymentCreditAsync(CurrentUserId, id, model.Code, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Create a PayPal order for a competition's entrance fee, for the frontend's PayPal Buttons.
    /// </summary>
    [HttpPost("{id:guid}/Register/PayPal/CreateOrder")]
    [Authorize]
    public async Task<ActionResult<PayPalOrderModel?>> CreatePayPalOrder(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userCompetitionService.CreatePayPalOrderAsync(id, cancellationToken);

        return FromResult(result.Map(order => new PayPalOrderModel { OrderId = order.OrderId }));
    }

    /// <summary>
    /// Capture a buyer-approved PayPal order and register the current user for the competition.
    /// </summary>
    [HttpPost("{id:guid}/Register/PayPal/Capture")]
    [Authorize]
    public async Task<ActionResult> CapturePayPalOrder(Guid id, CapturePayPalOrderModel model, CancellationToken cancellationToken)
    {
        var result = await _userCompetitionService.CapturePayPalOrderAsync(CurrentUserId, id, model.OrderId, cancellationToken);

        return FromResult(result);
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
    /// Get every competition either registered to or open for registration, most recently started first.
    /// </summary>
    [HttpGet("Registrations")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<UserCompetitionRegistrationListItem>>> GetRegistrations()
    {
        var registrations = await _competitionService.GetUserCompetitionRegistrationListAsync(CurrentUserId);

        return Ok(registrations);
    }

    /// <summary>
    /// Mark a competition as the current user's default, for it to be preselected on future logins.
    /// </summary>
    [HttpPut("{competitionId:guid}/SetDefault")]
    [Authorize]
    public async Task<ActionResult> SetDefault(Guid competitionId, CancellationToken cancellationToken)
    {
        var result = await _competitionService.SetDefaultCompetitionAsync(CurrentUserId, competitionId, cancellationToken);

        return FromResult(result);
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
    /// Get the league table for a competition as it would be had all of a user's predictions come true.
    /// </summary>
    [HttpGet("{id:guid}/UserLeagueTable/{userId:guid}")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<CompetitionUserLeagueTableItem>>> GetUserLeagueTable(Guid id, Guid userId)
    {
        return Ok(await _competitionService.CompetitionUserLeagueTableGetAsync(id, userId));
    }

    /// <summary>
    /// Get a user's league-position history for a competition, ordered by date ascending.
    /// </summary>
    [HttpGet("{id:guid}/UserLeagueHistory/{userId:guid}")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<UserCompetitionLeagueHistoryItem>>> GetUserLeagueHistory(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return Ok(await _competitionService.GetUserCompetitionLeagueHistoryAsync(userId, id, cancellationToken));
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

    /// <summary>
    /// Import a competition's full season fixture list (matches and team assignments) from the
    /// external data source configured via ExternalApiCompetitionCode, refining StartDate/EndDate to
    /// the actual fixture range. Safe to re-run - already-imported fixtures are skipped.
    /// </summary>
    /// <param name="id">The competition to import fixtures into.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id:guid}/ImportFixtures")]
    [Authorize(Roles = RoleConstants.CompetitionAdministrator)]
    public async Task<ActionResult<FixtureImportSummary?>> ImportFixtures(Guid id, CancellationToken cancellationToken)
    {
        var result = await _fixtureImportService.ImportSeasonAsync(id, cancellationToken);

        return FromResult(result);
    }
}
