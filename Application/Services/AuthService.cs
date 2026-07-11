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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _tokenService;
    private readonly IValidator<RegisterModel>? _registerValidator;
    private readonly IValidator<LoginModel>? _loginValidator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokenService,
        IValidator<RegisterModel>? registerValidator = null,
        IValidator<LoginModel>? loginValidator = null)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    /// <inheritdoc />
    public async Task<Result<AuthResultModel>> Register(RegisterModel model, CancellationToken cancellationToken = default)
    {
        if (_registerValidator is not null)
        {
            var validation = await _registerValidator.ValidateAsync(model, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new PropertyValidationError(e.PropertyName, e.ErrorMessage)).ToArray();
                return Result.Fail<AuthResultModel>(errors);
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
            return Result.Fail<AuthResultModel>(errors);
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Ok(_tokenService.GenerateToken(user, roles));
    }

    /// <inheritdoc />
    public async Task<Result<AuthResultModel>> Login(LoginModel model, CancellationToken cancellationToken = default)
    {
        if (_loginValidator is not null)
        {
            var validation = await _loginValidator.ValidateAsync(model, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new PropertyValidationError(e.PropertyName, e.ErrorMessage)).ToArray();
                return Result.Fail<AuthResultModel>(errors);
            }
        }

        var user = await _userManager.FindByNameAsync(model.UserName);

        // Deliberately generic message either way - don't reveal whether the username exists.
        if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            return Result.Fail<AuthResultModel>(new PropertyValidationError(string.Empty, "Invalid username or password."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Ok(_tokenService.GenerateToken(user, roles));
    }
}
