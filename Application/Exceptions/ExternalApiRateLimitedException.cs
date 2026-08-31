namespace Predictathon.Application.Exceptions;

/// <summary>
/// Thrown instead of calling the external match-data provider when doing so would exceed its
/// published rate limit. Declining locally keeps a burst of admin activity from spending the
/// budget the live-score poller needs, and turns an inevitable provider rejection into an error
/// that can say when to try again.
/// </summary>
public class ExternalApiRateLimitedException : Exception
{
    /// <summary>
    /// Creates a new <see cref="ExternalApiRateLimitedException"/>.
    /// </summary>
    /// <param name="retryAfter">How long until the provider's rate-limit window frees up a call.</param>
    public ExternalApiRateLimitedException(TimeSpan retryAfter)
        : base($"The football data provider's rate limit has been reached. Try again in {Math.Ceiling(retryAfter.TotalSeconds)} seconds.")
    {
        RetryAfter = retryAfter;
    }

    /// <summary>How long until the provider's rate-limit window frees up a call.</summary>
    public TimeSpan RetryAfter { get; }
}
