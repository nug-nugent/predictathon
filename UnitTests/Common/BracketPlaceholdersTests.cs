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
        source.Value.Reference.Should().Be(expectedGroup);
        source.Value.GroupPosition.Should().Be(expectedPosition);
    }

    [Theory]
    // The reference is the feeding match's Description, which is how one tie names another.
    [InlineData("Winner R16 3", "Round of 16 3")]
    [InlineData("Winner QF1", "Quarter-final 1")]
    [InlineData("Winner SF2", "Semi-final 2")]
    // "Winner R16 3" has a space and "Winner QF1" does not, in the same seeded competition.
    [InlineData("Winner QF 4", "Quarter-final 4")]
    [InlineData("winner sf 1", "Semi-final 1")]
    public void Parse_ReadsTheWinnerOfAnEarlierTie(string placeholder, string expectedReference)
    {
        var source = BracketPlaceholders.Parse(placeholder);

        source.Should().NotBeNull();
        source!.Value.Kind.Should().Be(BracketSourceKind.MatchWinner);
        source.Value.Reference.Should().Be(expectedReference);
    }

    [Fact]
    public void Parse_ReadsTheLoserOfAnEarlierTie()
    {
        // How a third-place play-off is filled, and the only use of the losing side.
        var source = BracketPlaceholders.Parse("Loser SF1");

        source.Should().NotBeNull();
        source!.Value.Kind.Should().Be(BracketSourceKind.MatchLoser);
        source.Value.Reference.Should().Be("Semi-final 1");
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

    [Fact]
    public void Parse_NamesRoundsTheSameWayKnockoutRoundsDoes()
    {
        // The reference has to match a Description the app itself would write, so it is built from
        // KnockoutRounds rather than spelled out again - rename a round there and this follows.
        BracketPlaceholders.Parse("Winner QF1")!.Value.Reference
            .Should().Be($"{KnockoutRounds.NameOf(8)} 1");
    }
}
