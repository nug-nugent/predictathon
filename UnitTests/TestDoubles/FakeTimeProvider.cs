namespace Predictathon.UnitTests.TestDoubles;

/// <summary>
/// A <see cref="TimeProvider"/> whose clock only moves when a test moves it, so window-based logic
/// (see ExternalApiRateLimiter) can be exercised across a minute without waiting one.
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _utcNow;

    /// <summary>Moves the clock forward.</summary>
    /// <param name="amount">How far to advance.</param>
    public void Advance(TimeSpan amount) => _utcNow += amount;
}
