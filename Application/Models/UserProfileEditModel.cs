namespace Predictathon.Application.Models;

/// <summary>
/// The full editable shape of a user's profile (Identity.Users), returned by both the load and
/// save endpoints behind /profile/edit.
/// </summary>
public class UserProfileEditModel
{
    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Forenames { get; set; }

    public string? Surname { get; set; }

    public string? FavouriteTeam { get; set; }

    public string? Location { get; set; }

    public string? Caption { get; set; }

    public string? ProfileText { get; set; }

    public int? EmailPredictionReminderDays { get; set; }

    public bool CanViewMessageboard { get; set; }

    public bool CanViewHiddenMessageThreads { get; set; }
}
