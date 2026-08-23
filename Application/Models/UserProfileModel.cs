namespace Predictathon.Application.Models;

/// <summary>
/// Publicly-viewable profile information for a user (Identity.Users).
/// </summary>
public class UserProfileModel
{
    public Guid UserID { get; set; }

    public string Username { get; set; } = "";

    public string? Caption { get; set; }

    public string? Location { get; set; }

    public string? FavouriteTeam { get; set; }

    public string? ProfileText { get; set; }

    public string? AvatarUrl { get; set; }

    /// <summary>
    /// The full-size version of the same picture, shown when the avatar is opened on the profile
    /// page. Null whenever <see cref="AvatarUrl"/> is.
    /// </summary>
    public string? AvatarLargeUrl { get; set; }
}
