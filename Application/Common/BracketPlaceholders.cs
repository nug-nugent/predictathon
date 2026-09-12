using System.Text.RegularExpressions;

namespace Predictathon.Application.Common;

/// <summary>
/// What a knockout match's <c>HomeTeamTBC</c> / <c>AwayTeamTBC</c> placeholder points at.
///
/// An undecided tie already says in words where its team comes from - "Winner Group A", "Winner
/// QF1" - because that is what a fixture list has to show a reader before the draw is made. Read
/// back, the same words say which group table or which earlier tie settles the slot, which is what
/// lets the bracket fill itself in as a tournament goes on.
///
/// The convention had been implicit in two scripts that each parsed it their own way. This is the
/// definition; <see cref="BracketPlaceholders"/> is the only thing that reads or writes it.
/// </summary>
public enum BracketSourceKind
{
    /// <summary>A finishing position in a group table - "Winner Group A", "Runner-up Group B".</summary>
    GroupPosition,

    /// <summary>The winner of an earlier tie - "Winner R16 3", "Winner QF1".</summary>
    MatchWinner,

    /// <summary>The loser of an earlier tie - "Loser SF1", which is how a play-off is filled.</summary>
    MatchLoser,
}

/// <summary>Where an undecided slot's team comes from, read off its placeholder.</summary>
/// <param name="Kind">Whether the slot is filled from a group table or from an earlier tie.</param>
/// <param name="GroupName">
/// The group's name ("Group A") for <see cref="BracketSourceKind.GroupPosition"/>, otherwise empty.
/// </param>
/// <param name="GroupPosition">
/// The finishing position wanted, 1 for the winner and 2 for the runner-up. Zero for the other kinds.
/// </param>
/// <param name="FeedingRound">
/// The <c>KnockoutRound</c> of the tie that settles this slot, for the two match kinds; zero for a
/// group position. Positions in the bracket rather than a description, because a competition's
/// descriptions are free text - a real World Cup's read "Last 16,  Atlanta" - while its round and
/// slot are the structure itself.
/// </param>
/// <param name="FeedingSlot">The <c>BracketSlot</c> of that tie; zero for a group position.</param>
public readonly record struct BracketSource(
    BracketSourceKind Kind,
    string GroupName,
    int GroupPosition,
    int FeedingRound,
    int FeedingSlot);

/// <summary>
/// Reads a knockout placeholder back into the thing that settles it, and writes the placeholder for
/// a slot whose feeder is known from the shape of the bracket.
/// </summary>
public static partial class BracketPlaceholders
{
    // Whitespace is generous throughout because the existing data isn't consistent about it - the
    // sample fixtures carry both "Winner R16 3" and "Winner QF1" - and because a placeholder is
    // typed by hand into an admin form, where a stray space is not a different meaning.
    [GeneratedRegex(@"^\s*winner\s+group\s+(?<group>\S+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex GroupWinnerPattern();

    [GeneratedRegex(@"^\s*runner[\s-]*up\s+group\s+(?<group>\S+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex GroupRunnerUpPattern();

    [GeneratedRegex(@"^\s*(?<outcome>winner|loser)\s*(?<round>r64|r32|r16|qf|sf)\s*(?<slot>\d+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex MatchOutcomePattern();

    /// <summary>
    /// The short form each round is written by, both ways round. R16 and below are named for their
    /// size; the last two rounds have names of their own, which is simply how football writes them.
    /// </summary>
    private static readonly Dictionary<string, int> RoundsByShortForm = new(StringComparer.OrdinalIgnoreCase)
    {
        ["R64"] = 64,
        ["R32"] = 32,
        ["R16"] = 16,
        ["QF"] = 8,
        ["SF"] = 4,
    };

    private static readonly Dictionary<int, string> ShortFormsByRound =
        RoundsByShortForm.ToDictionary(pair => pair.Value, pair => pair.Key);

    /// <summary>
    /// Reads a placeholder, or returns null where it says nothing this understands - which is not an
    /// error. A competition can put anything in that column, and an admin filling a slot by hand is
    /// always allowed; an unreadable placeholder just means nobody can fill it automatically.
    /// </summary>
    /// <param name="placeholder">The slot's placeholder text, possibly null or blank.</param>
    public static BracketSource? Parse(string? placeholder)
    {
        if (string.IsNullOrWhiteSpace(placeholder))
        {
            return null;
        }

        var groupWinner = GroupWinnerPattern().Match(placeholder);
        if (groupWinner.Success)
        {
            return new BracketSource(BracketSourceKind.GroupPosition, $"Group {groupWinner.Groups["group"].Value}", 1, 0, 0);
        }

        var groupRunnerUp = GroupRunnerUpPattern().Match(placeholder);
        if (groupRunnerUp.Success)
        {
            return new BracketSource(BracketSourceKind.GroupPosition, $"Group {groupRunnerUp.Groups["group"].Value}", 2, 0, 0);
        }

        var outcome = MatchOutcomePattern().Match(placeholder);
        if (outcome.Success
            && RoundsByShortForm.TryGetValue(outcome.Groups["round"].Value, out var feedingRound)
            && int.TryParse(outcome.Groups["slot"].Value, out var feedingSlot))
        {
            var kind = outcome.Groups["outcome"].Value.Equals("loser", StringComparison.OrdinalIgnoreCase)
                ? BracketSourceKind.MatchLoser
                : BracketSourceKind.MatchWinner;

            return new BracketSource(kind, "", 0, feedingRound, feedingSlot);
        }

        return null;
    }

    /// <summary>
    /// The placeholder for a slot fed by an earlier tie, or null for a round this has no short form
    /// for. The inverse of <see cref="Parse"/>, so anything written here reads back.
    /// </summary>
    /// <param name="feedingRound">The <c>KnockoutRound</c> of the tie that settles the slot.</param>
    /// <param name="feedingSlot">The <c>BracketSlot</c> of that tie.</param>
    /// <param name="wantWinner">Whether the slot takes that tie's winner rather than its loser.</param>
    public static string? Describe(int feedingRound, int feedingSlot, bool wantWinner)
        => ShortFormsByRound.TryGetValue(feedingRound, out var shortForm)
            ? $"{(wantWinner ? "Winner" : "Loser")} {shortForm} {feedingSlot}"
            : null;
}
