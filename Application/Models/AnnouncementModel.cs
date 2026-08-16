using Predictathon.Application.Constants;

namespace Predictathon.Application.Models;

/// <summary>
/// Model used when creating an announcement.
/// </summary>
public class CreateAnnouncementModel
{
    public string Content { get; set; } = "";

    public bool ShowOnLoginPage { get; set; }

    public bool ShowOnHomepage { get; set; } = true;

    /// <summary>
    /// One of <see cref="AnnouncementSeverities"/> - controls the styling the announcement is
    /// rendered with (e.g. amber for <see cref="AnnouncementSeverities.Warning"/>).
    /// </summary>
    public string Severity { get; set; } = AnnouncementSeverities.Info;

    public DateTime? ExpiryDateTimeUtc { get; set; }
}

// Full model including the generated identifier and audit fields, for the Announcements Admin page.
public class AnnouncementModel : CreateAnnouncementModel
{
    public int AnnouncementID { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Guid CreatedByUserID { get; set; }
}
