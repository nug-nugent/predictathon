using FluentResults;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Predictathon.Application.Attributes;
using Predictathon.Application.Common;
using Predictathon.Application.Constants;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Identity;

namespace Predictathon.Application.Services;

[ScopedService]
public class UserService : IUserService
{
    private readonly IAvatarService _avatarService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly ITrophyService _trophyService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;
    private readonly IValidator<UpdateProfileModel>? _updateProfileValidator;

    public UserService(
        IAvatarService avatarService,
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext dbContext,
        IEmailService emailService,
        ITrophyService trophyService,
        IConfiguration configuration,
        ILogger<UserService> logger,
        IValidator<UpdateProfileModel>? updateProfileValidator = null)
    {
        _avatarService = avatarService;
        _userManager = userManager;
        _dbContext = dbContext;
        _emailService = emailService;
        _trophyService = trophyService;
        _configuration = configuration;
        _logger = logger;
        _updateProfileValidator = updateProfileValidator;
    }

    /// <inheritdoc />
    public async Task<UserProfileModel?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var trophies = await _trophyService.GetForUserAsync(user.Id, cancellationToken);

        return new UserProfileModel
        {
            UserID = user.Id,
            Username = user.UserName ?? string.Empty,
            Caption = user.Caption,
            Location = user.Location,
            FavouriteTeam = user.FavouriteTeam,
            ProfileText = user.ProfileText,
            AvatarUrl = _avatarService.GetAvatarUrl(user.Id, user.ImageUploaded),
            AvatarLargeUrl = _avatarService.GetAvatarUrl(user.Id, user.ImageUploaded, large: true),
            Trophies = [.. trophies],
        };
    }

    /// <inheritdoc />
    public async Task<UserProfileEditModel?> GetProfileForEditAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        return user is null ? null : ToEditModel(user);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<PagedResult<UserAdminListItem>> GetUsersForAdminAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.Like(u.UserName!, pattern) ||
                EF.Functions.Like(u.Email!, pattern) ||
                (u.Forenames != null && EF.Functions.Like(u.Forenames, pattern)) ||
                (u.Surname != null && EF.Functions.Like(u.Surname, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();

        // Batch-loaded in one query rather than per-user, to avoid an N+1 against Identity.UserRoles.
        var rolesByUserId = await _dbContext.Query<IdentityUserRole<Guid>>()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_dbContext.Query<IdentityRole<Guid>>(), ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync(cancellationToken);

        var items = users.Select(u => new UserAdminListItem
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            Forenames = u.Forenames,
            Surname = u.Surname,
            Roles = rolesByUserId.Where(r => r.UserId == u.Id).Select(r => r.Name ?? string.Empty).ToList(),
            IsLockedOut = u.LockoutEnd is not null && u.LockoutEnd > DateTimeOffset.UtcNow,
            LastLoginDateTime = u.LastLoginDateTime,
        }).ToList();

        return new PagedResult<UserAdminListItem>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    /// <inheritdoc />
    public async Task<Result> UpdateUserRolesAsync(Guid userId, IReadOnlyList<string> roles, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var validRoles = new[] { RoleConstants.MatchAdministrator, RoleConstants.UserAdministrator, RoleConstants.CompetitionAdministrator };
        var unknownRoles = roles.Except(validRoles).ToList();
        if (unknownRoles.Count > 0)
        {
            return Result.Fail(new PropertyValidationError(nameof(roles), $"Unknown role(s): {string.Join(", ", unknownRoles)}."));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Fail(new NotFoundError("No such user."));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        if (userId == currentUserId
            && currentRoles.Contains(RoleConstants.UserAdministrator)
            && !roles.Contains(RoleConstants.UserAdministrator))
        {
            return Result.Fail(new PropertyValidationError(nameof(roles), "You cannot remove your own UserAdministrator role."));
        }

        var rolesToAdd = roles.Except(currentRoles).ToList();
        var rolesToRemove = currentRoles.Except(roles).ToList();

        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return Result.Fail(MapIdentityErrors(addResult, nameof(roles)));
            }
        }

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return Result.Fail(MapIdentityErrors(removeResult, nameof(roles)));
            }
        }

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> SetUserLockedAsync(Guid userId, bool locked, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (locked && userId == currentUserId)
        {
            return Result.Fail(new PropertyValidationError(string.Empty, "You cannot lock your own account."));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Fail(new NotFoundError("No such user."));
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null);
        if (!result.Succeeded)
        {
            return Result.Fail(MapIdentityErrors(result, string.Empty));
        }

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task SendPredictionEmailRemindersAsync(CancellationToken cancellationToken = default)
    {
        var overduePredictions = await _dbContext.CallStoredProcedureAsync<UserOverduePredictionsItem>(
            "UserOverduePredictionsGet", cancellationToken: cancellationToken);

        var frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? string.Empty).TrimEnd('/');
        var predictionsUrl = $"{frontendBaseUrl}/predictions";

        foreach (var userItems in overduePredictions.GroupBy(p => p.UserID))
        {
            var items = userItems.ToList();

            try
            {
                await _emailService.SendAsync(
                    items[0].EmailAddress,
                    "Prediction reminder",
                    BuildReminderEmailBody(items, predictionsUrl),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Don't mark these as reminded if the send failed, so they're picked up again on
                // the next scheduled run instead of being silently skipped. Other users still get
                // processed.
                _logger.LogError(ex, "Failed to send prediction reminder email to {UserId}", items[0].UserID);
                continue;
            }

            var userCompetitionIds = items.Select(i => i.UserCompetitionID).ToList();
            var userCompetitions = await _dbContext.UserCompetition
                .Where(uc => userCompetitionIds.Contains(uc.UserCompetitionID))
                .ToListAsync(cancellationToken);

            foreach (var userCompetition in userCompetitions)
            {
                userCompetition.LastEmailReminderSent = UkClock.Now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    // Compile-time-constant cell styles, built from the shared EmailStyle palette so the table
    // matches the branded shell EmailService wraps it in.
    private const string ReminderTableCellStyle = "padding:8px 12px;text-align:left;border-bottom:1px solid " + EmailStyle.FooterBorder + ";";
    private const string ReminderTableHeaderCellStyle = ReminderTableCellStyle + "font-size:12px;text-transform:uppercase;letter-spacing:0.03em;color:" + EmailStyle.FooterInk + ";background-color:" + EmailStyle.FooterBg + ";";

    private static string BuildReminderEmailBody(IReadOnlyList<UserOverduePredictionsItem> overduePredictions, string predictionsUrl)
    {
        var rows = string.Join(string.Empty, overduePredictions.Select((p, i) =>
        {
            var rowBg = i % 2 == 0 ? "#FFFFFF" : EmailStyle.FooterBg;
            return $"""<tr style="background-color:{rowBg};"><td style="{ReminderTableCellStyle}">{p.CompetitionName}</td><td style="{ReminderTableCellStyle}">{p.NextPredictionDue:dddd d MMMM 'at' h:mmtt}</td></tr>""";
        }));

        return $"""
            <p>Dear {overduePredictions[0].Username},</p>
            <p>You need to make a prediction in the following competition{(overduePredictions.Count > 1 ? "s" : "")}:</p>
            <table style="width:100%;border-collapse:collapse;border:1px solid {EmailStyle.FooterBorder};border-radius:6px;overflow:hidden;">
            <tr><th style="{ReminderTableHeaderCellStyle}">Competition</th><th style="{ReminderTableHeaderCellStyle}">Next prediction due</th></tr>
            {rows}
            </table>
            <p>Please visit <a href="{predictionsUrl}">{predictionsUrl}</a> and log in to register your predictions.</p>
            <p>Good luck,<br />Predictathon.</p>
            """;
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
