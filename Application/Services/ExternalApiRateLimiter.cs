using Microsoft.Extensions.Options;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Options;

namespace Predictathon.Application.Services;

/// <summary>
/// <see cref="IExternalApiRateLimiter"/> over a sliding one-minute window: it remembers when each
/// recent call was made and allows a new one only while fewer than the budget fall inside the last
/// sixty seconds.
///
/// A sliding window rather than a fixed one because that's what the provider enforces - under fixed
/// buckets, ten calls at 10:00:59 and ten more at 10:01:01 look fine locally and get rejected by
/// them. Registered by hand as a singleton in Program.cs rather than via [ScopedService]: a
/// per-request instance would count each request's calls in isolation and enforce nothing.
/// </summary>
public sealed class ExternalApiRateLimiter : IExternalApiRateLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly TimeProvider _timeProvider;
    private readonly int _budget;
    private readonly Queue<DateTimeOffset> _calls = new();
    private readonly Lock _gate = new();

    /// <summary>
    /// Initialises a new instance of the <see cref="ExternalApiRateLimiter"/> class.
    /// </summary>
    /// <param name="options">Supplies the provider's per-minute limit and the headroom to leave under it.</param>
    /// <param name="timeProvider">Clock the window is measured against; injected so tests don't have to wait a real minute.</param>
    public ExternalApiRateLimiter(IOptions<FootballDataApiOptions> options, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        // At least one call has to be allowed however the two settings are configured, or a typo in
        // appsettings silently turns the integration off altogether instead of failing loudly.
        _budget = Math.Max(1, options.Value.RequestsPerMinute - options.Value.RequestsPerMinuteHeadroom);
    }

    /// <inheritdoc />
    public bool TryAcquire()
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            Expire(now);

            if (_calls.Count >= _budget)
            {
                return false;
            }

            _calls.Enqueue(now);
            return true;
        }
    }

    /// <inheritdoc />
    public TimeSpan TimeUntilNextSlot()
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            Expire(now);

            if (_calls.Count < _budget)
            {
                return TimeSpan.Zero;
            }

            // The oldest call in the window is the first to fall out of it, so that's when the next
            // slot appears. Never negative: Expire has already dropped anything older than the window.
            return _calls.Peek() + Window - now;
        }
    }

    /// <summary>
    /// Drops calls that have fallen out of the window. Safe as a plain loop from the front because
    /// the queue is in call order, so everything expired is at the head.
    /// </summary>
    /// <param name="now">The current time.</param>
    private void Expire(DateTimeOffset now)
    {
        while (_calls.Count > 0 && now - _calls.Peek() >= Window)
        {
            _calls.Dequeue();
        }
    }
}
