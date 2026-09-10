namespace Predictathon.Application.Common;

/// <summary>
/// When a match's result becomes eligible to be confirmed. A result entered while the players are
/// still on the pitch would score everyone's predictions against a scoreline that hasn't settled,
/// so a match has to be comfortably over before anything - an admin on the Process Results page, or
/// the auto-processor working from the provider's final score - can confirm it.
/// </summary>
public static class MatchResultWindow
{
    /// <summary>
    /// How long after kick-off a result can first be confirmed. Ninety minutes rather than a figure
    /// allowing for stoppages: it's the point past which a scoreline is worth trusting at all, and
    /// the provider's own "finished" is what actually decides an automatic confirmation.
    /// </summary>
    public const int EligibleMinutesAfterKickoff = 90;

    /// <summary>
    /// The latest kick-off whose result is eligible to be confirmed at the given moment - anything
    /// later hasn't been going long enough.
    /// </summary>
    /// <param name="now">The current UK wall-clock time (see <see cref="UkClock"/>).</param>
    public static DateTime EligibleFrom(DateTime now) => now.AddMinutes(-EligibleMinutesAfterKickoff);

    /// <summary>
    /// Whether a match that kicked off at the given time can have its result confirmed yet.
    /// </summary>
    /// <param name="matchDateTime">The match's kick-off time.</param>
    /// <param name="now">The current UK wall-clock time (see <see cref="UkClock"/>).</param>
    public static bool IsEligible(DateTime matchDateTime, DateTime now)
        => matchDateTime <= EligibleFrom(now);
}
