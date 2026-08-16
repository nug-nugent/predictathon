namespace Predictathon.Application.Constants;

/// <summary>
/// Announcement.Severity values. Stored as plain text (mirroring FixtureChangeProposalStatuses)
/// rather than a lookup table, since there are only ever these two fixed values.
/// </summary>
public static class AnnouncementSeverities
{
    public const string Info = "Info";

    public const string Warning = "Warning";
}
