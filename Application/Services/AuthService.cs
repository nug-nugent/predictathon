using FluentResults;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly IAvatarService _avatarService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IValidator<RegisterModel>? _registerValidator;
    private readonly IValidator<LoginModel>? _loginValidator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IAvatarService avatarService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IValidator<RegisterModel>? registerValidator = null,
        IValidator<LoginModel>? loginValidator = null)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _avatarService = avatarService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
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
            Email = model.Email,
            Forenames = model.Forenames,
            Surname = model.Surname
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => new PropertyValidationError(string.Empty, e.Description)).ToArray();
            return Result.Fail<AuthTokenResult>(errors);
        }

        var tokenResult = await IssueTokensAsync(user, model.RememberMe, cancellationToken);
        await SendWelcomeEmailAsync(user, cancellationToken);

        return Result.Ok(tokenResult);
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

        if (user is not null && await _userManager.IsLockedOutAsync(user))
        {
            // Deliberately distinct from the generic "invalid username or password" message below -
            // a locked-out user isn't mistyping anything, and should know to contact an admin
            // instead of retrying. Checked before HasPasswordAsync/CheckPasswordAsync since it
            // doesn't matter whether their credentials would otherwise be valid.
            return Result.Fail<AuthTokenResult>(new PropertyValidationError(string.Empty, "This account has been disabled. Please contact an administrator."));
        }

        if (user is not null && !await _userManager.HasPasswordAsync(user))
        {
            // A real account (e.g. migrated from the legacy dbo.User table) with no usable
            // password yet - a distinct failure from wrong credentials, so the frontend can point
            // them at password reset instead of implying they mistyped something.
            return Result.Fail<AuthTokenResult>(new PasswordResetRequiredError());
        }

        // Deliberately generic message either way - don't reveal whether the username exists.
        if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            if (user is not null)
            {
                // UserManager.CheckPasswordAsync alone doesn't drive Identity's lockout machinery -
                // only SignInManager does that automatically, and this JWT-based flow doesn't use
                // SignInManager. Record the failure explicitly, mirroring what SignInManager does
                // internally, so repeated wrong passwords still lock the account out.
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user))
                {
                    return Result.Fail<AuthTokenResult>(new PropertyValidationError(string.Empty, "Too many failed login attempts. Please try again later."));
                }
            }

            return Result.Fail<AuthTokenResult>(new PropertyValidationError(string.Empty, "Invalid username or password."));
        }

        if (user.AccessFailedCount > 0)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
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
        var response = _tokenService.GenerateToken(user, roles);
        response.AvatarUrl = _avatarService.GetAvatarUrl(user.Id, user.ImageUploaded);

        return Result.Ok(response);
    }

    /// <inheritdoc />
    public async Task Logout(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _refreshTokenService.RevokeAsync(refreshToken, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ForgotPassword(ForgotPasswordModel model, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(model.UserNameOrEmail)
            ?? await _userManager.FindByEmailAsync(model.UserNameOrEmail);

        // Always succeed regardless of whether a match was found - same "don't reveal whether the
        // account exists" reasoning as Login.
        if (user is null || string.IsNullOrEmpty(user.Email))
        {
            return Result.Ok();
        }

        await SendPasswordResetEmailAsync(user, cancellationToken);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> AdminResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || string.IsNullOrEmpty(user.Email))
        {
            return Result.Fail(new NotFoundError("No such user."));
        }

        await SendPasswordResetEmailAsync(user, cancellationToken);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> ResetPassword(ResetPasswordModel model, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(model.UserId.ToString());
        if (user is null)
        {
            return Result.Fail(new PropertyValidationError(string.Empty, "This password reset link is invalid or has expired."));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new PropertyValidationError(string.Empty, e.Description)).ToArray();
            return Result.Fail(errors);
        }

        return Result.Ok();
    }

    private async Task SendWelcomeEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? string.Empty).TrimEnd('/');
        var predictionsUrl = $"{frontendBaseUrl}/predictions";

        try
        {
            await _emailService.SendAsync(
                user.Email!,
                "Welcome to Predictathon",
                $"""<p>Dear {user.Forenames},</p><p>Thanks for signing up to Predictathon. Your username is <strong>{user.UserName}</strong>.</p><p>Visit <a href="{predictionsUrl}">{predictionsUrl}</a> to enter your first predictions, and use the 'Edit profile' menu to add a profile picture and set your email reminder preferences.</p><p>Good luck,<br />Predictathon.</p>""",
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Registration has already succeeded and tokens have already been issued by this point -
            // don't fail the whole request just because the welcome email couldn't be sent.
            _logger.LogError(ex, "Failed to send welcome email to {UserId}", user.Id);
        }
    }

    private async Task SendPasswordResetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? string.Empty).TrimEnd('/');
        var resetUrl = $"{frontendBaseUrl}/password-reset/confirm?userId={user.Id}&token={Uri.EscapeDataString(token)}";

        await _emailService.SendAsync(
            user.Email!,
            "Reset your Predictathon password",
            $"""<p>Click the link below to reset your Predictathon password:</p><p><a href="{resetUrl}">{resetUrl}</a></p><p>If you didn't request this, you can safely ignore this email.</p>""",
            cancellationToken);
    }

    private async Task<AuthTokenResult> IssueTokensAsync(ApplicationUser user, bool rememberMe, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var response = _tokenService.GenerateToken(user, roles);
        response.AvatarUrl = _avatarService.GetAvatarUrl(user.Id, user.ImageUploaded);

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
