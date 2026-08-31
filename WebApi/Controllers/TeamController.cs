using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Constants;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize]
public class TeamController : ApiControllerBase
{
    private readonly ITeamService _teamService;

    public TeamController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    /// <summary>
    /// Get the teams registered for a competition, ordered by name.
    /// </summary>
    [HttpGet("{competitionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<TeamModel>>> GetForCompetition(Guid competitionId, CancellationToken cancellationToken)
    {
        var teams = await _teamService.GetForCompetitionAsync(competitionId, cancellationToken);

        return Ok(teams);
    }

    /// <summary>
    /// Get the teams assigned to a competition including their TeamCompetitionID, ordered by name.
    /// </summary>
    [HttpGet("{competitionId:guid}/Assigned")]
    [Authorize(Roles = RoleConstants.CompetitionAdministrator)]
    public async Task<ActionResult<IReadOnlyList<TeamCompetitionModel>>> GetAssignedForCompetition(Guid competitionId, CancellationToken cancellationToken)
    {
        var teams = await _teamService.GetAssignedForCompetitionAsync(competitionId, cancellationToken);

        return Ok(teams);
    }

    /// <summary>
    /// Get teams not yet assigned to a competition, ordered by name (for populating an "add team" selector).
    /// </summary>
    [HttpGet("{competitionId:guid}/Unassigned")]
    [Authorize(Roles = RoleConstants.CompetitionAdministrator)]
    public async Task<ActionResult<IReadOnlyList<TeamModel>>> GetUnassignedForCompetition(Guid competitionId, CancellationToken cancellationToken)
    {
        var teams = await _teamService.GetUnassignedForCompetitionAsync(competitionId, cancellationToken);

        return Ok(teams);
    }

    /// <summary>
    /// Get a team's played-match stats, results, upcoming fixtures and (for competitions without
    /// knockout matches) the competition's league table, for the Team Detail page.
    /// </summary>
    [HttpGet("{competitionId:guid}/{teamId:guid}/Detail")]
    public async Task<ActionResult<TeamDetailModel?>> GetDetail(Guid competitionId, Guid teamId, CancellationToken cancellationToken)
    {
        var detail = await _teamService.GetTeamDetailAsync(competitionId, teamId, CurrentUserId, cancellationToken);

        return OkOrNotFound(detail);
    }

    /// <summary>
    /// Get a team's most recently played matches in a competition, newest first - the quick
    /// recent-form list shown from a team's name, without the whole Team Detail payload.
    /// </summary>
    [HttpGet("{competitionId:guid}/{teamId:guid}/RecentResults")]
    public async Task<ActionResult<IReadOnlyList<TeamRecentResultItem>>> GetRecentResults(Guid competitionId, Guid teamId, CancellationToken cancellationToken, [FromQuery] int count = 6)
    {
        var results = await _teamService.GetRecentResultsAsync(competitionId, teamId, count, cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Assign a team to a competition.
    /// </summary>
    [HttpPost("{competitionId:guid}/{teamId:guid}")]
    [Authorize(Roles = RoleConstants.CompetitionAdministrator)]
    public async Task<ActionResult> AddToCompetition(Guid competitionId, Guid teamId, CancellationToken cancellationToken)
    {
        var result = await _teamService.AddToCompetitionAsync(competitionId, teamId, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Remove a team's assignment from a competition.
    /// </summary>
    [HttpDelete("{teamCompetitionId:guid}")]
    [Authorize(Roles = RoleConstants.CompetitionAdministrator)]
    public async Task<ActionResult> RemoveFromCompetition(Guid teamCompetitionId, CancellationToken cancellationToken)
    {
        var result = await _teamService.RemoveFromCompetitionAsync(teamCompetitionId, cancellationToken);

        return FromResult(result);
    }
}
