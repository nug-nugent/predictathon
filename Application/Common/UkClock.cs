namespace Predictathon.Application.Common;

/// <summary>
/// Current time in the UK, matching the naive local wall-clock semantics MatchDateTime is stored
/// and compared in throughout the app (see PredictionService's own save-cutoff check).
/// </summary>
public static class UkClock
{
    private static readonly TimeZoneInfo UkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UkTimeZone);
}
