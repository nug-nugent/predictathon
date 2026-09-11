using System.Text.RegularExpressions;

namespace Predictathon.Application.Common;

/// <summary>
/// What a knockout match's <c>HomeTeamTBC</c> / <c>AwayTeamTBC</c> placeholder points at.
///
/// An undecided tie already says in words where its team comes from - "Winner Group A", "Winner
/// QF1" - because that is what a fixture list has to show a reader before the draw is made. Read
/// back, the same words say which group table or which earlier match settles the slot, which is
/// what lets the bracket fill itself in as a tournament goes on.
///
/// The convention had been implicit in two scripts that each parsed it their own way. This is the
/// definition; <see cref="BracketPlaceholders"/> is the only thing that reads it.
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
/// <param name="Reference">
/// The group's name ("Group A") for <see cref="BracketSourceKind.GroupPosition"/>, otherwise the
/// feeding match's <c>Description</c> ("Quarter-final 1") - which is how a match is named here,
/// there being no foreign key between a tie and the ties that feed it.
/// </param>
/// <param name="GroupPosition">
/// The finishing position wanted, 1 for the winner and 2 for the runner-up. Zero where
/// <see cref="Kind"/> is not <see cref="BracketSourceKind.GroupPosition"/>.
/// </param>
public readonly record struct BracketSource(BracketSourceKind Kind, string Reference, int GroupPosition);

/// <summary>
/// Reads a knockout placeholder back into the thing that settles it.
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

    [GeneratedRegex(@"^\s*(?<outcome>winner|loser)\s*(?<round>r16|qf|sf)\s*(?<number>\d+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex MatchOutcomePattern();

    /// <summary>
    /// The round each short form names, mapped to a <c>KnockoutRound</c> so the full description is
    /// built by <see cref="KnockoutRounds.NameOf"/> rather than spelled out again here. Rename a
    /// round there and these keep pointing at the right matches.
    /// </summary>
    private static readonly Dictionary<string, int> RoundsByShortForm = new(StringComparer.OrdinalIgnoreCase)
    {
        ["R16"] = 16,
        ["QF"] = 8,
        ["SF"] = 4,
    };

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
            return new BracketSource(BracketSourceKind.GroupPosition, $"Group {groupWinner.Groups["group"].Value}", 1);
        }

        var groupRunnerUp = GroupRunnerUpPattern().Match(placeholder);
        if (groupRunnerUp.Success)
        {
            return new BracketSource(BracketSourceKind.GroupPosition, $"Group {groupRunnerUp.Groups["group"].Value}", 2);
        }

        var outcome = MatchOutcomePattern().Match(placeholder);
        if (outcome.Success && RoundsByShortForm.TryGetValue(outcome.Groups["round"].Value, out var knockoutRound))
        {
            var kind = outcome.Groups["outcome"].Value.Equals("loser", StringComparison.OrdinalIgnoreCase)
                ? BracketSourceKind.MatchLoser
                : BracketSourceKind.MatchWinner;

            return new BracketSource(kind, $"{KnockoutRounds.NameOf(knockoutRound)} {outcome.Groups["number"].Value}", 0);
        }

        return null;
    }
}
