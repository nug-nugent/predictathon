using FluentResults;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Exceptions;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
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
    private readonly IApplicationDbContext _dbContext;
    private readonly IAvatarService _avatarService;
    private readonly IValidator<RegisterModel>? _registerValidator;
    private readonly IValidator<LoginModel>? _loginValidator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IApplicationDbContext dbContext,
        IAvatarService avatarService,
        IValidator<RegisterModel>? registerValidator = null,
        IValidator<LoginModel>? loginValidator = null)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _dbContext = dbContext;
        _avatarService = avatarService;
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

        // Self-healing: covers any account whose dbo.User row wasn't created at registration
        // time (e.g. it predates this fix) - the next silent refresh backfills it.
        await EnsureDboUserAsync(user, cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var response = _tokenService.GenerateToken(user, roles);
        response.AvatarUrl = _avatarService.GetAvatarUrl(user.Id, await HasUploadedImageAsync(user.Id, cancellationToken));

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

    private async Task<AuthTokenResult> IssueTokensAsync(ApplicationUser user, bool rememberMe, CancellationToken cancellationToken)
    {
        // dbo.User is a separate, legacy table that every reporting feature (league tables, Hall
        // of Fame, statistics, prediction history) reads from - Identity's own store has no
        // knowledge of it. Registration used to leave a new account without a matching row at
        // all, making them invisible everywhere else. Ensuring it here (and on refresh) closes
        // that gap without a one-off migration step - a deliberate stopgap until dbo.User is
        // migrated out entirely.
        await EnsureDboUserAsync(user, cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var response = _tokenService.GenerateToken(user, roles);
        response.AvatarUrl = _avatarService.GetAvatarUrl(user.Id, await HasUploadedImageAsync(user.Id, cancellationToken));

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

    private async Task EnsureDboUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.User.AnyAsync(u => u.UserID == user.Id, cancellationToken);
        if (exists)
        {
            return;
        }

        var dboUser = new Domain.Entities.User
        {
            UserID = user.Id,
            Username = user.UserName ?? user.Id.ToString(),
            // dbo.User.Password is a vestigial NOT NULL column from before Identity existed -
            // Identity's own ApplicationUser.PasswordHash is the only credential store now.
            Password = "(managed by Identity)",
            EmailAddress = user.Email,
            // The column's DB-level DEFAULT ((1)) never applies here - EF Core always sends the
            // CLR value (false) explicitly in the INSERT, so this has to be set here to actually
            // grant new users messageboard access by default, matching legacy behaviour.
            CanViewMessageboard = true,
        };

        await _dbContext.AddAsync(dboUser, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            // Another concurrent request (e.g. two tabs logging in at once) already created it.
        }
    }

    private async Task<bool> HasUploadedImageAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.User.FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);
        return user?.ImageUploaded ?? false;
    }
}
