namespace Predictathon.Application.Common;

/// <summary>
/// The meaning of <c>dbo.Match.KnockoutRound</c>, in one place.
///
/// The column is an ordering key valued as the number of teams contesting the round - 16 for the
/// round of 16, 8 for the quarter-finals, and so on down to 2 for the final - so a plain descending
/// sort runs the bracket from its first round through to its last. The third-place play-off isn't
/// part of the bracket tree and takes <see cref="ThirdPlacePlayOffRound"/>, which is not a power of
/// two and so can never collide with a real round.
///
/// Because of that one sentinel the value must never be treated as arithmetic - no dividing it by
/// two to count a round's matches. Everything the bracket layout needs comes from a round's
/// position in the sorted sequence and from how many matches are actually in it.
/// </summary>
public static class KnockoutRounds
{
    /// <summary>
    /// The value <c>KnockoutRound</c> carries for a third-place play-off. Sits between the
    /// semi-finals (4) and the final (2) so it sorts where it is played.
    /// </summary>
    public const int ThirdPlacePlayOffRound = 3;

    private static readonly Dictionary<int, string> RoundNames = new()
    {
        [64] = "Round of 64",
        [32] = "Round of 32",
        [16] = "Round of 16",
        [8] = "Quarter final",
        [4] = "Semi final",
        [ThirdPlacePlayOffRound] = "Third place play-off",
        [2] = "Final",
    };

    /// <summary>
    /// The display name for a knockout round, falling back to naming it by its size where a
    /// competition uses a round this doesn't know about.
    /// </summary>
    /// <param name="knockoutRound">The round's <c>KnockoutRound</c> value.</param>
    public static string NameOf(int knockoutRound)
        => RoundNames.TryGetValue(knockoutRound, out var name) ? name : $"Round of {knockoutRound}";

    /// <summary>
    /// Whether a round value marks the third-place play-off rather than a round of the tree.
    /// </summary>
    /// <param name="knockoutRound">The round's <c>KnockoutRound</c> value.</param>
    public static bool IsThirdPlacePlayOff(int knockoutRound)
        => knockoutRound == ThirdPlacePlayOffRound;

    /// <summary>
    /// Whether a sequence of tree rounds, largest first, is a well-formed single-elimination
    /// bracket: each round exactly half the size of the one before it, ending at the final, and
    /// every round holding the number of matches its size implies.
    ///
    /// The bracket view's geometry assumes all of that, so a competition whose rounds don't line up
    /// - a half-numbered bracket, a round nobody finished filling in - is better shown as a plain
    /// list than as a tree with holes in it.
    /// </summary>
    /// <param name="rounds">The tree rounds, largest first, paired with how many matches each holds.</param>
    public static bool IsWellFormedTree(IReadOnlyList<(int KnockoutRound, int MatchCount)> rounds)
    {
        if (rounds.Count == 0 || rounds[^1].KnockoutRound != 2)
        {
            return false;
        }

        for (var index = 0; index < rounds.Count; index++)
        {
            var (knockoutRound, matchCount) = rounds[index];

            // A round of n teams is n/2 matches, all of which have to be there.
            if (knockoutRound != matchCount * 2)
            {
                return false;
            }

            // And each round halves the one before it - no gaps, no repeats.
            if (index > 0 && rounds[index - 1].KnockoutRound != knockoutRound * 2)
            {
                return false;
            }
        }

        return true;
    }
}
