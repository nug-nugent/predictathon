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

    public DateTime CreatedAtUtc { get; set; }
}
