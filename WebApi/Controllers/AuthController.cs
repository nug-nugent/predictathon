using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPost("Register")]
    public async Task<ActionResult<AuthResultModel?>> Register(RegisterModel model, CancellationToken cancellationToken)
    {
        var result = await _authService.Register(model, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Log in with a username and password.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPost("Login")]
    public async Task<ActionResult<AuthResultModel?>> Login(LoginModel model, CancellationToken cancellationToken)
    {
        var result = await _authService.Login(model, cancellationToken);

        return FromResult(result);
    }
}
