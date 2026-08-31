namespace Predictathon.Application.Common;

/// <summary>
/// The span of matches the Home page's Today's Matches section (and the Live page behind it) covers:
/// everything kicking off today, plus anything that kicked off shortly before midnight and could
/// still be in play. Without that carry-over a late-night match would vanish from the section at
/// midnight, mid-game.
/// </summary>
public static class LiveDayWindow
{
    /// <summary>
    /// How far back before today's start a still-unplayed match can have kicked off and still be
    /// carried into today's window. Long enough to cover a match plus stoppages, extra time and
    /// penalties, short enough that a fixture whose result nobody has entered for days doesn't
    /// linger on the Home page forever.
    /// </summary>
    public const int CarryOverHours = 6;

    /// <summary>
    /// The earliest kick-off the window includes for the given moment.
    /// </summary>
    /// <param name="now">The current UK wall-clock time (see <see cref="UkClock"/>).</param>
    public static DateTime Start(DateTime now)
    {
        var startOfToday = now.Date;
        var carryOverStart = now.AddHours(-CarryOverHours);

        return carryOverStart < startOfToday ? carryOverStart : startOfToday;
    }

    /// <summary>
    /// The latest kick-off the window includes for the given moment - the end of today.
    /// </summary>
    /// <param name="now">The current UK wall-clock time (see <see cref="UkClock"/>).</param>
    public static DateTime End(DateTime now)
    {
        // A whole second short of midnight rather than .999: MatchDateTime is a SQL DATETIME, whose
        // ~3.33ms resolution rounds 23:59:59.999 up into the next day.
        return now.Date.AddDays(1).AddSeconds(-1);
    }

    /// <summary>
    /// Whether a match belongs in the window. Matches from before today are only carried over
    /// while they're still unresolved - last night's confirmed results belong on the Results page,
    /// not in today's live section.
    /// </summary>
    /// <param name="matchDateTime">The match's kick-off time.</param>
    /// <param name="matchPlayed">Whether the match's result has been confirmed.</param>
    /// <param name="now">The current UK wall-clock time (see <see cref="UkClock"/>).</param>
    public static bool Includes(DateTime matchDateTime, bool matchPlayed, DateTime now)
    {
        if (matchDateTime < Start(now) || matchDateTime > End(now))
        {
            return false;
        }

        return matchDateTime >= now.Date || !matchPlayed;
    }
}
