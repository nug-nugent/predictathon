using FluentAssertions;
using Predictathon.Application.Common;

namespace Predictathon.UnitTests.Common;

/// <summary>
/// Covers the rule behind the Home page's Live updates section: which matches count as part of
/// "today", including the carry-over that stops a late-night match disappearing at midnight while
/// it's still being played.
/// </summary>
public class LiveDayWindowTests
{
    private static readonly DateTime Afternoon = new DateTime(2026, 8, 22, 15, 30, 0);
    private static readonly DateTime JustAfterMidnight = new DateTime(2026, 8, 22, 0, 20, 0);

    [Fact]
    public void Start_IsMidnight_WhenTodayHasAlreadyRunLongerThanTheCarryOver()
    {
        LiveDayWindow.Start(Afternoon).Should().Be(new DateTime(2026, 8, 22, 0, 0, 0));
    }

    [Fact]
    public void Start_ReachesBackIntoYesterday_WhenTodayIsYoungerThanTheCarryOver()
    {
        LiveDayWindow.Start(JustAfterMidnight).Should().Be(new DateTime(2026, 8, 21, 18, 20, 0));
    }

    [Fact]
    public void End_IsTheLastSecondOfToday()
    {
        // A whole second short of midnight, not .999 - MatchDateTime is a SQL DATETIME, whose
        // ~3.33ms resolution would round 23:59:59.999 up into tomorrow.
        LiveDayWindow.End(Afternoon).Should().Be(new DateTime(2026, 8, 22, 23, 59, 59));
    }

    [Theory]
    [InlineData(9, 0, false)]   // this morning, already played
    [InlineData(9, 0, true)]    // this morning, still unresolved
    [InlineData(20, 0, false)]  // tonight, not played yet
    public void Includes_AcceptsAnyOfTodaysMatches_PlayedOrNot(int hour, int minute, bool matchPlayed)
    {
        var kickoff = new DateTime(2026, 8, 22, hour, minute, 0);

        LiveDayWindow.Includes(kickoff, matchPlayed, Afternoon).Should().BeTrue();
    }

    [Fact]
    public void Includes_RejectsTomorrowsMatches()
    {
        LiveDayWindow.Includes(new DateTime(2026, 8, 23, 12, 0, 0), matchPlayed: false, Afternoon).Should().BeFalse();
    }

    [Fact]
    public void Includes_CarriesLastNightsMatchOver_WhileItsResultIsUnconfirmed()
    {
        var lateKickoff = new DateTime(2026, 8, 21, 22, 45, 0);

        LiveDayWindow.Includes(lateKickoff, matchPlayed: false, JustAfterMidnight).Should().BeTrue();
    }

    [Fact]
    public void Includes_DropsLastNightsMatch_OnceItsResultIsConfirmed()
    {
        // A confirmed result from yesterday belongs on the Results page, not in today's section.
        var lateKickoff = new DateTime(2026, 8, 21, 22, 45, 0);

        LiveDayWindow.Includes(lateKickoff, matchPlayed: true, JustAfterMidnight).Should().BeFalse();
    }

    [Fact]
    public void Includes_DropsAnUnresolvedMatchOlderThanTheCarryOver()
    {
        // Otherwise a fixture nobody ever entered a result for would sit on the Home page forever.
        var staleKickoff = new DateTime(2026, 8, 21, 12, 0, 0);

        LiveDayWindow.Includes(staleKickoff, matchPlayed: false, JustAfterMidnight).Should().BeFalse();
    }
}
