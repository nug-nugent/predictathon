namespace Predictathon.Application.Models;

/// <summary>
/// Model used to update a user's profile. Applies to Identity.Users, the sole source of truth
/// for user data now that the legacy dbo.User table has been retired.
/// </summary>
public class UpdateProfileModel
{
    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Forenames { get; set; }

    public string? Surname { get; set; }

    public string? FavouriteTeam { get; set; }

    public string? Location { get; set; }

    public string? Caption { get; set; }

    public string? ProfileText { get; set; }

    public int? EmailPredictionReminderDays { get; set; }

    /// <summary>
    /// Only persisted when the caller (not necessarily the profile's owner) holds the
    /// UserAdministrator role - see UserController.UpdateProfileEdit.
    /// </summary>
    public bool CanViewMessageboard { get; set; }

    /// <summary>
    /// See <see cref="CanViewMessageboard"/>.
    /// </summary>
    public bool CanViewHiddenMessageThreads { get; set; }
}
