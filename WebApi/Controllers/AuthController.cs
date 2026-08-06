using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Predictathon.Application.Constants;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

public class AuthController : ApiControllerBase
{
    private const string RefreshTokenCookieName = "RefreshToken";

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// The path the refresh-token cookie is scoped to, so the browser only sends it to the
    /// endpoints that need it. Built from <see cref="HttpRequest.PathBase"/> rather than a fixed
    /// "/api/Auth" string, since the browser evaluates a cookie's Path against the full URL it's
    /// calling - which includes any IIS virtual-application segment (e.g. "/api") that ASP.NET
    /// Core's own routing has already stripped away by the time this code runs.
    /// </summary>
    private string RefreshTokenCookiePath => $"{Request.PathBase}/Auth";

    /// <summary>
    /// Register a new user.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPost("Register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResultModel?>> Register(RegisterModel model, CancellationToken cancellationToken)
    {
        var result = await _authService.Register(model, cancellationToken);

        return HandleTokenResult(result, model.RememberMe);
    }

    /// <summary>
    /// Log in with a username and password.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPost("Login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResultModel?>> Login(LoginModel model, CancellationToken cancellationToken)
    {
        var result = await _authService.Login(model, cancellationToken);

        return HandleTokenResult(result, model.RememberMe);
    }

    /// <summary>
    /// Exchanges the refresh token cookie for a fresh access token. Does not require (or accept)
    /// an existing access token - that's the whole point, it's how you get a new one.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPost("Refresh")]
    public async Task<ActionResult<AuthResultModel?>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        var result = await _authService.RefreshToken(refreshToken, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Revokes the current refresh token (if any) and clears its cookie.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPost("Logout")]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        await _authService.Logout(refreshToken, cancellationToken);

        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = RefreshTokenCookiePath });

        return NoContent();
    }

    /// <summary>
    /// Requests a password-reset email. Always returns 204, regardless of whether the given
    /// username/email matched an account - see AuthService.ForgotPassword.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPost("ForgotPassword")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordModel model, CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPassword(model, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Completes a password reset using the token emailed by ForgotPassword.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPost("ResetPassword")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult> ResetPassword(ResetPasswordModel model, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPassword(model, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Admin-triggered password reset email for a known user, from the User Admin page.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    [HttpPost("AdminResetPassword/{userId:guid}")]
    [Authorize(Roles = RoleConstants.UserAdministrator)]
    public async Task<ActionResult> AdminResetPassword(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _authService.AdminResetPasswordAsync(userId, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Sets the refresh-token cookie on success and returns just the access-token portion of the
    /// result to the client - the raw refresh token must never appear in a JSON response body,
    /// only in the (HttpOnly) cookie, otherwise client-side JS could read it.
    /// </summary>
    private ActionResult<AuthResultModel?> HandleTokenResult(Result<AuthTokenResult> result, bool rememberMe)
    {
        if (result.IsFailed)
        {
            return FromResult(Result.Fail<AuthResultModel>(result.Errors));
        }

        SetRefreshTokenCookie(result.Value, rememberMe);

        return Ok(result.Value.Response);
    }

    private void SetRefreshTokenCookie(AuthTokenResult tokenResult, bool rememberMe)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            // None (not Lax/Strict) so this keeps working regardless of whether the frontend and
            // API end up same-site or genuinely cross-site in production - requires Secure, which
            // we already have (HTTPS-only dev cert, and production is expected to be HTTPS too).
            SameSite = SameSiteMode.None,
            Path = RefreshTokenCookiePath
        };

        // Persistent cookie only when "remember me" was checked; otherwise a session cookie
        // (no Expires at all) that the browser clears on close. The DB-side token still has its
        // own (shorter) expiry as a backstop either way - see AuthService.IssueTokensAsync.
        if (rememberMe)
        {
            options.Expires = tokenResult.RefreshTokenExpiresAtUtc;
        }

        Response.Cookies.Append(RefreshTokenCookieName, tokenResult.RefreshToken, options);
    }
}
