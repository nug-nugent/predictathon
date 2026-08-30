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

    /// <summary>
    /// Converts a stored UK wall-clock time (e.g. a MatchDateTime) back to UTC, for talking to an
    /// external API that works in UTC. The inverse of <see cref="ToUkLocal"/>.
    /// </summary>
    /// <param name="ukLocal">A naive UK wall-clock time.</param>
    public static DateTime ToUtc(DateTime ukLocal) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(ukLocal, DateTimeKind.Unspecified), UkTimeZone);
}
