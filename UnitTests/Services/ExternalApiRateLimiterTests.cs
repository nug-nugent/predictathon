using FluentAssertions;
using Microsoft.Extensions.Options;
using Predictathon.Application.Options;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;

namespace Predictathon.UnitTests.Services;

/// <summary>
/// Covers the local guard that keeps the app from making football-data.org calls it already knows
/// will be rejected - the budget it allows, the headroom it holds back, and the fact that the window
/// slides rather than resetting on the minute.
/// </summary>
public class ExternalApiRateLimiterTests
{
    private static readonly DateTimeOffset Start = new(2026, 8, 22, 15, 0, 0, TimeSpan.Zero);

    private static (ExternalApiRateLimiter Limiter, FakeTimeProvider Clock) MakeLimiter(
        int requestsPerMinute = 10,
        int headroom = 2)
    {
        var clock = new FakeTimeProvider(Start);
        var options = Options.Create(new FootballDataApiOptions
        {
            RequestsPerMinute = requestsPerMinute,
            RequestsPerMinuteHeadroom = headroom,
        });

        return (new ExternalApiRateLimiter(options, clock), clock);
    }

    [Fact]
    public void TryAcquire_AllowsTheConfiguredLimitLessHeadroom_ThenRefuses()
    {
        var (limiter, _) = MakeLimiter(requestsPerMinute: 10, headroom: 2);

        var granted = Enumerable.Range(0, 10).Count(_ => limiter.TryAcquire());

        granted.Should().Be(8, "two of the provider's ten calls are held back for a second worker process");
    }

    [Fact]
    public void TryAcquire_AlwaysAllowsOneCall_WhenHeadroomWouldSwallowTheWholeBudget()
    {
        // A misconfiguration should degrade to "slow", not to "the integration is silently off".
        var (limiter, _) = MakeLimiter(requestsPerMinute: 2, headroom: 5);

        limiter.TryAcquire().Should().BeTrue();
        limiter.TryAcquire().Should().BeFalse();
    }

    [Fact]
    public void TryAcquire_FreesSlotsOneAtATimeAsTheyAgeOut_NotAllAtOnce()
    {
        var (limiter, clock) = MakeLimiter(requestsPerMinute: 3, headroom: 0);

        // Three calls ten seconds apart: 15:00:00, 15:00:10, 15:00:20.
        limiter.TryAcquire().Should().BeTrue();
        clock.Advance(TimeSpan.FromSeconds(10));
        limiter.TryAcquire().Should().BeTrue();
        clock.Advance(TimeSpan.FromSeconds(10));
        limiter.TryAcquire().Should().BeTrue();

        limiter.TryAcquire().Should().BeFalse();

        // At 15:01:00 only the first has fallen out of the trailing minute, so exactly one slot
        // opens up - a fixed-bucket limiter would wrongly hand back all three here.
        clock.Advance(TimeSpan.FromSeconds(40));
        limiter.TryAcquire().Should().BeTrue();
        limiter.TryAcquire().Should().BeFalse();
    }

    [Fact]
    public void TimeUntilNextSlot_IsZero_WhileTheBudgetHasRoom()
    {
        var (limiter, _) = MakeLimiter(requestsPerMinute: 3, headroom: 0);

        limiter.TryAcquire();

        limiter.TimeUntilNextSlot().Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void TimeUntilNextSlot_CountsDownToTheOldestCallLeavingTheWindow()
    {
        var (limiter, clock) = MakeLimiter(requestsPerMinute: 1, headroom: 0);

        limiter.TryAcquire();
        clock.Advance(TimeSpan.FromSeconds(20));

        limiter.TimeUntilNextSlot().Should().Be(TimeSpan.FromSeconds(40));
    }
}
