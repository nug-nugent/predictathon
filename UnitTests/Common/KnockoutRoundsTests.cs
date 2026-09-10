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
    [InlineData(8, "Quarter-final")]
    [InlineData(4, "Semi-final")]
    [InlineData(2, "Final")]
    public void NameOf_NamesEachRoundOfTheTree(int knockoutRound, string expected)
    {
        KnockoutRounds.NameOf(knockoutRound).Should().Be(expected);
    }

    [Fact]
    public void NameOf_NamesTheSentinelAsTheThirdPlacePlayOff()
    {
        KnockoutRounds.NameOf(KnockoutRounds.ThirdPlacePlayOffRound).Should().Be("3rd place playoff");
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

    /// A round of the right size with its slots numbered 1..n, which is what a sound bracket is
    /// made of. Kept as a helper so each test below reads as the one thing it is breaking.
    private static BracketRoundShape Round(int knockoutRound)
        => new(knockoutRound, [.. Enumerable.Range(1, knockoutRound / 2).Select(slot => (int?)slot)]);

    [Fact]
    public void DescribeProblems_AcceptsAWholeBracket()
    {
        KnockoutRounds.DescribeProblems([Round(16), Round(8), Round(4), Round(2)]).Should().BeEmpty();
    }

    [Fact]
    public void DescribeProblems_AcceptsABracketThatStartsPartWayIn()
    {
        // A competition whose knockout stage opens at the quarter-finals is still a whole tree.
        KnockoutRounds.DescribeProblems([Round(8), Round(4), Round(2)]).Should().BeEmpty();
    }

    [Fact]
    public void DescribeProblems_ReportsARoundMissingMatches()
    {
        // Seven of the eight last-16 ties present, which is what a half-entered draw looks like.
        var problems = KnockoutRounds.DescribeProblems(
            [new BracketRoundShape(16, [1, 2, 3, 4, 5, 6, 7]), Round(8), Round(4), Round(2)]);

        problems.Should().ContainSingle()
            .Which.Should().Be("Round of 16: 7 matches, where this round needs 8.");
    }

    [Fact]
    public void DescribeProblems_ReportsMatchesWithNoSlotYet()
    {
        // The state an imported bracket arrives in: rounds known, draw positions not yet numbered.
        var problems = KnockoutRounds.DescribeProblems(
            [new BracketRoundShape(8, [1, 2, null, null]), Round(4), Round(2)]);

        problems.Should().ContainSingle()
            .Which.Should().Be("Quarter-final: 2 matches with no bracket slot.");
    }

    [Fact]
    public void DescribeProblems_SaysMatchRatherThanMatchesForASingleOne()
    {
        var problems = KnockoutRounds.DescribeProblems(
            [new BracketRoundShape(4, [1, null]), Round(2)]);

        problems.Should().ContainSingle()
            .Which.Should().Be("Semi-final: 1 match with no bracket slot.");
    }

    [Fact]
    public void DescribeProblems_ReportsADuplicatedSlotAndTheGapItLeaves()
    {
        // The typo this whole method exists for: two ties numbered 4, so slot 5 goes unfilled and
        // the tree still draws - just with a tie in the wrong half, and nothing to say so.
        var problems = KnockoutRounds.DescribeProblems(
            [new BracketRoundShape(16, [1, 2, 3, 4, 4, 6, 7, 8]), Round(8), Round(4), Round(2)]);

        problems.Should().BeEquivalentTo([
            "Round of 16: slot 4 is used by 2 matches.",
            "Round of 16: no match is in slot 5.",
        ]);
    }

    [Fact]
    public void DescribeProblems_ReportsASlotOutsideTheRound()
    {
        var problems = KnockoutRounds.DescribeProblems(
            [new BracketRoundShape(8, [1, 2, 3, 9]), Round(4), Round(2)]);

        problems.Should().Contain("Quarter-final: slot 9 is outside the range 1 to 4.");
    }

    [Fact]
    public void DescribeProblems_LeavesUnreachedSlotsAloneWhileARoundIsStillBeingFilledIn()
    {
        // A round part-numbered shouldn't also be told that every slot nobody has got to yet is
        // missing - that is one fault reported twice, and the noisier half is the wrong one.
        var problems = KnockoutRounds.DescribeProblems(
            [new BracketRoundShape(8, [1, 2, null, null]), Round(4), Round(2)]);

        problems.Should().NotContain(problem => problem.Contains("no match is in slot"));
    }

    [Fact]
    public void DescribeProblems_ReportsAGapBetweenRounds()
    {
        // The quarter-finals missing entirely, so nothing feeds the semi-finals.
        var problems = KnockoutRounds.DescribeProblems([Round(16), Round(4), Round(2)]);

        problems.Should().ContainSingle()
            .Which.Should().Be("Quarter-final is missing - Round of 16 is followed straight by Semi-final.");
    }

    [Fact]
    public void DescribeProblems_ReportsATreeThatDoesNotReachTheFinal()
    {
        var problems = KnockoutRounds.DescribeProblems([Round(16), Round(8)]);

        problems.Should().ContainSingle()
            .Which.Should().Be("The bracket has no final - it stops at the Quarter-final.");
    }

    [Fact]
    public void DescribeProblems_ReportsNoRoundsAtAll()
    {
        KnockoutRounds.DescribeProblems([]).Should().ContainSingle()
            .Which.Should().Be("No match has been given a bracket round yet.");
    }
}
