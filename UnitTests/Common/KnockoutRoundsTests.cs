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

    [Theory]
    // How a real World Cup's fixtures read in this database: the round in free text, the venue
    // after it, and no other record of which round a tie belongs to anywhere on the row.
    [InlineData("Last 32,  Dallas", 32)]
    [InlineData("Last 16,  Atlanta", 16)]
    [InlineData("Quarter final,  Boston", 8)]
    [InlineData("Semi final,  Dallas", 4)]
    [InlineData("Third place play-off,  Miami", 3)]
    [InlineData("Final,  New York/New Jersey", 2)]
    // A description that got typed twice over, which is the sort of thing real data does.
    [InlineData("Semi final,  Semi final, Dallas", 4)]
    // And how this site writes its own, which the same reader has to cope with.
    [InlineData("Round of 16 1", 16)]
    [InlineData("Quarter-final 1", 8)]
    [InlineData("Semi-final 2", 4)]
    [InlineData("3rd place playoff", 3)]
    [InlineData("Final", 2)]
    public void RoundFromDescription_ReadsTheRoundOutOfFreeText(string description, int expected)
    {
        KnockoutRounds.RoundFromDescription(description).Should().Be(expected);
    }

    [Fact]
    public void RoundFromDescription_DoesNotLetQuarterOrSemiFinalsReadAsTheFinal()
    {
        // The whole reason the patterns are anchored. "Quarter final" contains "final", and a
        // careless match would quietly file every quarter-final as the final itself - a bracket
        // that then looks well-formed and is completely wrong.
        KnockoutRounds.RoundFromDescription("Quarter final,  Boston").Should().Be(8);
        KnockoutRounds.RoundFromDescription("Semi final,  Dallas").Should().Be(4);
        KnockoutRounds.RoundFromDescription("Third place play-off,  Miami").Should().Be(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Group A")]
    [InlineData("Matchday 3")]
    // A size this site has no round for - better to say nothing than to invent "Round of 12".
    [InlineData("Last 12, Nowhere")]
    public void RoundFromDescription_ReturnsNullWhenTheDescriptionNamesNoRound(string? description)
    {
        KnockoutRounds.RoundFromDescription(description).Should().BeNull();
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
