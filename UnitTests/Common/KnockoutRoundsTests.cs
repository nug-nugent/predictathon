using FluentAssertions;
using Predictathon.Application.Common;

namespace Predictathon.UnitTests.Common;

/// <summary>
/// Covers the one place that reads meaning into dbo.Match.KnockoutRound - the round names, and the
/// check that a set of rounds actually forms a bracket. Both matter because the column is an
/// ordering key with a sentinel in it rather than a plain count, and the bracket view's whole
/// geometry assumes a well-formed tree.
/// </summary>
public class KnockoutRoundsTests
{
    [Theory]
    [InlineData(32, "Round of 32")]
    [InlineData(16, "Round of 16")]
    [InlineData(8, "Quarter Final")]
    [InlineData(4, "Semi Final")]
    [InlineData(2, "Final")]
    public void NameOf_NamesEachRoundOfTheTree(int knockoutRound, string expected)
    {
        KnockoutRounds.NameOf(knockoutRound).Should().Be(expected);
    }

    [Fact]
    public void NameOf_NamesTheSentinelAsTheThirdPlacePlayOff()
    {
        KnockoutRounds.NameOf(KnockoutRounds.ThirdPlacePlayOffRound).Should().Be("Third Place Play-off");
    }

    [Fact]
    public void ThirdPlacePlayOffRound_IsNotAPowerOfTwo_SoItCannotCollideWithARealRound()
    {
        var round = KnockoutRounds.ThirdPlacePlayOffRound;

        (round & (round - 1)).Should().NotBe(0, "a real round always contains a power-of-two number of teams");
    }

    [Fact]
    public void ThirdPlacePlayOffRound_SortsBetweenTheSemiFinalsAndTheFinal()
    {
        // Descending KnockoutRound is the bracket's running order, and the play-off is played
        // after the semi-finals and before the final.
        KnockoutRounds.ThirdPlacePlayOffRound.Should().BeLessThan(4).And.BeGreaterThan(2);
    }

    [Fact]
    public void IsThirdPlacePlayOff_OnlyMatchesTheSentinel()
    {
        KnockoutRounds.IsThirdPlacePlayOff(KnockoutRounds.ThirdPlacePlayOffRound).Should().BeTrue();
        KnockoutRounds.IsThirdPlacePlayOff(4).Should().BeFalse();
        KnockoutRounds.IsThirdPlacePlayOff(2).Should().BeFalse();
    }

    [Fact]
    public void IsWellFormedTree_AcceptsAWholeBracket()
    {
        KnockoutRounds.IsWellFormedTree([(16, 8), (8, 4), (4, 2), (2, 1)]).Should().BeTrue();
    }

    [Fact]
    public void IsWellFormedTree_AcceptsABracketThatStartsPartWayIn()
    {
        // A competition whose knockout stage opens at the quarter-finals is still a whole tree.
        KnockoutRounds.IsWellFormedTree([(8, 4), (4, 2), (2, 1)]).Should().BeTrue();
    }

    [Fact]
    public void IsWellFormedTree_RejectsARoundMissingMatches()
    {
        // Seven of the eight last-16 ties numbered, which is what a half-finished draw looks like.
        KnockoutRounds.IsWellFormedTree([(16, 7), (8, 4), (4, 2), (2, 1)]).Should().BeFalse();
    }

    [Fact]
    public void IsWellFormedTree_RejectsAGapBetweenRounds()
    {
        // The quarter-finals missing entirely, so nothing feeds the semi-finals.
        KnockoutRounds.IsWellFormedTree([(16, 8), (4, 2), (2, 1)]).Should().BeFalse();
    }

    [Fact]
    public void IsWellFormedTree_RejectsATreeThatDoesNotReachTheFinal()
    {
        KnockoutRounds.IsWellFormedTree([(16, 8), (8, 4)]).Should().BeFalse();
    }

    [Fact]
    public void IsWellFormedTree_RejectsNoRoundsAtAll()
    {
        KnockoutRounds.IsWellFormedTree([]).Should().BeFalse();
    }
}
