namespace Predictathon.Application.Common;

/// <summary>
/// Current time in the UK, matching the naive local wall-clock semantics MatchDateTime is stored
/// and compared in throughout the app (see PredictionService's own save-cutoff check).
/// </summary>
public static class UkClock
{
    private static readonly TimeZoneInfo UkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UkTimeZone);

    /// <summary>
    /// Converts a UTC instant (e.g. from an external API) to the naive UK wall-clock time
    /// MatchDateTime is stored and compared in.
    /// </summary>
    public static DateTime ToUkLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), UkTimeZone);
}
