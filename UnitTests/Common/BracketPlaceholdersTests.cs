using FluentAssertions;
using Predictathon.Application.Common;

namespace Predictathon.UnitTests.Common;

public class BracketPlaceholdersTests
{
    [Theory]
    [InlineData("Winner Group A", "Group A", 1)]
    [InlineData("Winner Group H", "Group H", 1)]
    [InlineData("Runner-up Group B", "Group B", 2)]
    // The sample fixtures and the admin form disagree about spacing and case, and neither is wrong.
    [InlineData("winner group a", "Group a", 1)]
    [InlineData("  Runner up Group C  ", "Group C", 2)]
    public void Parse_ReadsAGroupPosition(string placeholder, string expectedGroup, int expectedPosition)
    {
        var source = BracketPlaceholders.Parse(placeholder);

        source.Should().NotBeNull();
        source!.Value.Kind.Should().Be(BracketSourceKind.GroupPosition);
        source.Value.GroupName.Should().Be(expectedGroup);
        source.Value.GroupPosition.Should().Be(expectedPosition);
    }

    [Theory]
    // A feeder is named by where it sits in the bracket, not by what it is called: "Winner QF1" is
    // the winner of round 8, slot 1. That is what lets this work against a competition whose
    // descriptions read "Last 16,  Atlanta".
    [InlineData("Winner R32 5", 32, 5)]
    [InlineData("Winner R16 3", 16, 3)]
    [InlineData("Winner QF1", 8, 1)]
    [InlineData("Winner SF2", 4, 2)]
    // "Winner R16 3" has a space and "Winner QF1" does not, in the same seeded competition.
    [InlineData("Winner QF 4", 8, 4)]
    [InlineData("winner sf 1", 4, 1)]
    public void Parse_ReadsTheWinnerOfAnEarlierTie(string placeholder, int expectedRound, int expectedSlot)
    {
        var source = BracketPlaceholders.Parse(placeholder);

        source.Should().NotBeNull();
        source!.Value.Kind.Should().Be(BracketSourceKind.MatchWinner);
        source.Value.FeedingRound.Should().Be(expectedRound);
        source.Value.FeedingSlot.Should().Be(expectedSlot);
    }

    [Fact]
    public void Parse_ReadsTheLoserOfAnEarlierTie()
    {
        // How a third-place play-off is filled, and the only use of the losing side.
        var source = BracketPlaceholders.Parse("Loser SF1");

        source.Should().NotBeNull();
        source!.Value.Kind.Should().Be(BracketSourceKind.MatchLoser);
        source.Value.FeedingRound.Should().Be(4);
        source.Value.FeedingSlot.Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Winner of the play-off")]
    [InlineData("3rd Group A/B/F")]
    [InlineData("TBC")]
    public void Parse_ReturnsNullForAnythingItCannotRead(string? placeholder)
    {
        // Not an error. A competition can put what it likes in that column - the Euros' four best
        // third-placed teams genuinely can't be named this way - and an unreadable placeholder just
        // means an admin fills that slot in themselves.
        BracketPlaceholders.Parse(placeholder).Should().BeNull();
    }

    [Theory]
    [InlineData(32, 5, true, "Winner R32 5")]
    [InlineData(16, 3, true, "Winner R16 3")]
    [InlineData(8, 1, true, "Winner QF 1")]
    [InlineData(4, 2, false, "Loser SF 2")]
    public void Describe_WritesAPlaceholderThatParseReadsBack(int round, int slot, bool wantWinner, string expected)
    {
        var placeholder = BracketPlaceholders.Describe(round, slot, wantWinner);

        placeholder.Should().Be(expected);

        // The round trip is the point: anything generated has to be readable by the same code that
        // resolves a hand-typed one, or the two halves of this drift apart.
        var source = BracketPlaceholders.Parse(placeholder);
        source.Should().NotBeNull();
        source!.Value.FeedingRound.Should().Be(round);
        source.Value.FeedingSlot.Should().Be(slot);
        source.Value.Kind.Should().Be(wantWinner ? BracketSourceKind.MatchWinner : BracketSourceKind.MatchLoser);
    }

    [Fact]
    public void Describe_SaysNothingForARoundWithNoShortForm()
    {
        // The final has no short form because nothing is ever fed by it, and the play-off's
        // sentinel round isn't a round of the tree at all.
        BracketPlaceholders.Describe(2, 1, wantWinner: true).Should().BeNull();
        BracketPlaceholders.Describe(KnockoutRounds.ThirdPlacePlayOffRound, 1, wantWinner: true).Should().BeNull();
    }
}
