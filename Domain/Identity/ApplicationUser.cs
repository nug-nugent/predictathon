using Microsoft.AspNetCore.Identity;

namespace Predictathon.Domain.Identity;

/// <summary>
/// The application's Identity user. Backed by the Identity.Users table (SSDT-managed) - the sole
/// source of truth for user data now that the legacy dbo.User table has been retired and its
/// accounts migrated in (see the dbo.User pre-deployment script).
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string? Forenames { get; set; }

    public string? Surname { get; set; }

    public string? FavouriteTeam { get; set; }

    public string? Location { get; set; }

    public string? Caption { get; set; }

    public string? ProfileText { get; set; }

    public DateTime? LastLoginDateTime { get; set; }

    public bool ImageUploaded { get; set; }

    public int TotalMessageboardPosts { get; set; }

    public int? EmailPredictionReminderDays { get; set; }

    public bool CanViewHiddenMessageThreads { get; set; }

    public bool CanViewMessageboard { get; set; } = true;
}
