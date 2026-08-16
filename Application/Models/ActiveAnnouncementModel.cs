using Predictathon.Application.Constants;

namespace Predictathon.Application.Models;

/// <summary>
/// A single announcement as shown on the public feed (homepage and/or login page) - omits admin-only
/// fields like <see cref="AnnouncementModel.CreatedByUserID"/>.
/// </summary>
public class ActiveAnnouncementModel
{
    public int AnnouncementID { get; set; }

    public string Content { get; set; } = "";

    public bool ShowOnLoginPage { get; set; }

    public bool ShowOnHomepage { get; set; }

    /// <summary>
    /// One of <see cref="AnnouncementSeverities"/> - controls the styling the announcement is
    /// rendered with (e.g. amber for <see cref="AnnouncementSeverities.Warning"/>).
    /// </summary>
    public string Severity { get; set; } = AnnouncementSeverities.Info;

    public DateTime CreatedAtUtc { get; set; }
}
