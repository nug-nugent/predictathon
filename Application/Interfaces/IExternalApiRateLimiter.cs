namespace Predictathon.Application.Interfaces;

/// <summary>
/// Tracks how many calls have been made to the external match-data provider inside its rolling
/// rate-limit window, so the app can decline to call rather than make a request it knows will be
/// rejected. Registered as a singleton: the count has to be shared by every caller in the process -
/// the live-score poller and any admin running a fixture import draw on one budget.
/// </summary>
public interface IExternalApiRateLimiter
{
    /// <summary>
    /// Takes one call out of the current window's budget, returning false if none is left. Never
    /// blocks or waits: a caller that can't have a slot is expected to give up and try later, since
    /// waiting for one on a request thread just moves the failure.
    /// </summary>
    bool TryAcquire();

    /// <summary>
    /// How long until at least one call frees up. <see cref="TimeSpan.Zero"/> when the budget isn't
    /// exhausted. Used to tell an admin when their import will work rather than just refusing it.
    /// </summary>
    TimeSpan TimeUntilNextSlot();
}
