using Microsoft.AspNetCore.Identity;

namespace Predictathon.Domain.Identity;

/// <summary>
/// The application's Identity user. Backed by the new AspNetUsers table (SSDT-managed), which is
/// entirely separate from the legacy dbo.User table - no data migration between the two has happened yet.
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
