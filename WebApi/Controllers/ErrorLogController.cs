using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Constants;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

[Authorize(Roles = RoleConstants.UserAdministrator)]
public class ErrorLogController : ApiControllerBase
{
    private readonly IErrorLogService _errorLogService;

    public ErrorLogController(IErrorLogService errorLogService)
    {
        _errorLogService = errorLogService;
    }

    /// <summary>
    /// Gets a server-paged list of logged errors/warnings, newest first, for the admin Error Log page.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of rows per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ErrorLogListItem>>> GetErrors([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await _errorLogService.GetErrorsAsync(page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize, cancellationToken);

        return Ok(result);
    }
}
