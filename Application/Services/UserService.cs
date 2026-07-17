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
public class UserService : IUserService
{
    private readonly IAvatarService _avatarService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<UpdateProfileModel>? _updateProfileValidator;

    public UserService(
        IAvatarService avatarService,
        UserManager<ApplicationUser> userManager,
        IValidator<UpdateProfileModel>? updateProfileValidator = null)
    {
        _avatarService = avatarService;
        _userManager = userManager;
        _updateProfileValidator = updateProfileValidator;
    }

    public async Task<UserProfileModel?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        return new UserProfileModel
        {
            UserID = user.Id,
            Username = user.UserName ?? string.Empty,
            Caption = user.Caption,
            Location = user.Location,
            FavouriteTeam = user.FavouriteTeam,
            ProfileText = user.ProfileText,
            AvatarUrl = _avatarService.GetAvatarUrl(user.Id, user.ImageUploaded),
        };
    }

    public async Task<UserProfileEditModel?> GetProfileForEditAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        return user is null ? null : ToEditModel(user);
    }

    public async Task<Result<UserProfileEditModel>> UpdateProfileAsync(Guid userId, UpdateProfileModel model, bool allowAdminFields, CancellationToken cancellationToken = default)
    {
        if (_updateProfileValidator is not null)
        {
            var validation = await _updateProfileValidator.ValidateAsync(model, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new PropertyValidationError(e.PropertyName, e.ErrorMessage)).ToArray();
                return Result.Fail<UserProfileEditModel>(errors);
            }
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Fail<UserProfileEditModel>(new NotFoundError("No such user."));
        }

        var usernameChanged = !string.Equals(user.UserName, model.UserName, StringComparison.Ordinal);
        var emailChanged = !string.Equals(user.Email, model.Email, StringComparison.Ordinal);

        // Checked upfront, before either Set*Async call below - both of those persist immediately
        // (each internally does its own save), so if we let one succeed and then failed validating
        // the other, the first change would already be committed despite the overall Result being a
        // failure. Pre-checking both means a validation failure here leaves nothing half-written.
        if (usernameChanged)
        {
            var existing = await _userManager.FindByNameAsync(model.UserName);
            if (existing is not null && existing.Id != userId)
            {
                return Result.Fail<UserProfileEditModel>(new PropertyValidationError(nameof(model.UserName), $"Username '{model.UserName}' is already taken."));
            }
        }

        if (emailChanged)
        {
            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing is not null && existing.Id != userId)
            {
                return Result.Fail<UserProfileEditModel>(new PropertyValidationError(nameof(model.Email), $"Email '{model.Email}' is already taken."));
            }
        }

        if (usernameChanged)
        {
            var setUserNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
            if (!setUserNameResult.Succeeded)
            {
                return Result.Fail<UserProfileEditModel>(MapIdentityErrors(setUserNameResult, nameof(model.UserName)));
            }
        }

        if (emailChanged)
        {
            var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
            if (!setEmailResult.Succeeded)
            {
                return Result.Fail<UserProfileEditModel>(MapIdentityErrors(setEmailResult, nameof(model.Email)));
            }
        }

        user.Forenames = model.Forenames;
        user.Surname = model.Surname;
        user.FavouriteTeam = model.FavouriteTeam;
        user.Location = model.Location;
        user.Caption = model.Caption;
        user.ProfileText = model.ProfileText;
        user.EmailPredictionReminderDays = model.EmailPredictionReminderDays;

        // Matches legacy: these two flags are only ever changed by a UserAdministrator, regardless
        // of whose profile is being edited (even a self-edit by an admin goes through this gate).
        if (allowAdminFields)
        {
            user.CanViewMessageboard = model.CanViewMessageboard;
            user.CanViewHiddenMessageThreads = model.CanViewHiddenMessageThreads;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Fail<UserProfileEditModel>(MapIdentityErrors(updateResult, string.Empty));
        }

        return Result.Ok(ToEditModel(user));
    }

    private static UserProfileEditModel ToEditModel(ApplicationUser user) => new()
    {
        UserId = user.Id,
        UserName = user.UserName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        Forenames = user.Forenames,
        Surname = user.Surname,
        FavouriteTeam = user.FavouriteTeam,
        Location = user.Location,
        Caption = user.Caption,
        ProfileText = user.ProfileText,
        EmailPredictionReminderDays = user.EmailPredictionReminderDays,
        CanViewMessageboard = user.CanViewMessageboard,
        CanViewHiddenMessageThreads = user.CanViewHiddenMessageThreads,
    };

    private static PropertyValidationError[] MapIdentityErrors(IdentityResult result, string fallbackPropertyName)
        => result.Errors.Select(e => new PropertyValidationError(GuessPropertyName(e.Code) ?? fallbackPropertyName, e.Description)).ToArray();

    private static string? GuessPropertyName(string identityErrorCode) => identityErrorCode switch
    {
        _ when identityErrorCode.Contains("UserName", StringComparison.Ordinal) => "UserName",
        _ when identityErrorCode.Contains("Email", StringComparison.Ordinal) => "Email",
        _ => null,
    };
}
