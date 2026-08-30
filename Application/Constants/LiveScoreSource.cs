namespace Predictathon.Application.Constants;

/// <summary>
/// Where a row in dbo.MatchLiveScore's current scoreline came from. Stored as text rather than an
/// enum so the column reads for itself in the database, matching how dbo.Announcement.Severity is
/// handled.
/// </summary>
public static class LiveScoreSource
{
    /// <summary>Imported from the external match-data provider.</summary>
    public const string Api = "Api";

    /// <summary>Entered by a match administrator, typically to correct or get ahead of the feed.</summary>
    public const string Admin = "Admin";
}
