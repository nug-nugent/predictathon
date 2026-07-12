using FluentResults;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.Domain.Identity;

namespace Predictathon.Application.Services;

[ScopedService]
public class AuthService : IAuthService
{
    // "Remember me" -> a persistent refresh token that survives a browser restart.
    // Otherwise -> a short server-side cap as a backstop, even though the cookie itself is
    // session-scoped (some browsers/extensions don't reliably clear session cookies on close).
    private static readonly TimeSpan RememberedRefreshTokenLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan UnrememberedRefreshTokenLifetime = TimeSpan.FromDays(1);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IValidator<RegisterModel>? _registerValidator;
    private readonly IValidator<LoginModel>? _loginValidator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IValidator<RegisterModel>? registerValidator = null,
        IValidator<LoginModel>? loginValidator = null)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    /// <inheritdoc />
    public async Task<Result<AuthTokenResult>> Register(RegisterModel model, CancellationToken cancellationToken = default)
    {
        if (_registerValidator is not null)
        {
            var validation = await _registerValidator.ValidateAsync(model, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new PropertyValidationError(e.PropertyName, e.ErrorMessage)).ToArray();
                return Result.Fail<AuthTokenResult>(errors);
            }
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => new PropertyValidationError(string.Empty, e.Description)).ToArray();
            return Result.Fail<AuthTokenResult>(errors);
        }

        return Result.Ok(await IssueTokensAsync(user, model.RememberMe, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Result<AuthTokenResult>> Login(LoginModel model, CancellationToken cancellationToken = default)
    {
        if (_loginValidator is not null)
        {
            var validation = await _loginValidator.ValidateAsync(model, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new PropertyValidationError(e.PropertyName, e.ErrorMessage)).ToArray();
                return Result.Fail<AuthTokenResult>(errors);
            }
        }

        var user = await _userManager.FindByNameAsync(model.UserName)
            ?? await _userManager.FindByEmailAsync(model.UserName);

        // Deliberately generic message either way - don't reveal whether the username exists.
        if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            return Result.Fail<AuthTokenResult>(new PropertyValidationError(string.Empty, "Invalid username or password."));
        }

        return Result.Ok(await IssueTokensAsync(user, model.RememberMe, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Result<AuthResultModel>> RefreshToken(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result.Fail<AuthResultModel>(new UnauthorizedError("No refresh token was supplied."));
        }

        var userId = await _refreshTokenService.ValidateAsync(refreshToken, cancellationToken);
        if (userId is null)
        {
            return Result.Fail<AuthResultModel>(new UnauthorizedError("The refresh token is invalid or has expired."));
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
        {
            return Result.Fail<AuthResultModel>(new UnauthorizedError("The refresh token is invalid or has expired."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Ok(_tokenService.GenerateToken(user, roles));
    }

    /// <inheritdoc />
    public async Task Logout(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _refreshTokenService.RevokeAsync(refreshToken, cancellationToken);
        }
    }

    private async Task<AuthTokenResult> IssueTokensAsync(ApplicationUser user, bool rememberMe, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var response = _tokenService.GenerateToken(user, roles);

        var refreshLifetime = rememberMe ? RememberedRefreshTokenLifetime : UnrememberedRefreshTokenLifetime;
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.Add(refreshLifetime);
        var refreshToken = await _refreshTokenService.GenerateAsync(user.Id, refreshTokenExpiresAtUtc, cancellationToken);

        return new AuthTokenResult
        {
            Response = response,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
        };
    }
}
