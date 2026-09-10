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
        [8] = "Quarter-final",
        [4] = "Semi-final",
        [ThirdPlacePlayOffRound] = "3rd place playoff",
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
    /// Everything wrong with a bracket, said plainly enough for an admin to act on, or an empty
    /// list where there is nothing wrong.
    ///
    /// A single-elimination bracket has to hold together in two ways at once. Its rounds have to
    /// halve - each one exactly half the size of the one before it, ending at the final, every
    /// round holding the number of matches its size implies. And within a round the draw positions
    /// have to be a complete set: 1 to n, each used once. The view's geometry assumes both, and
    /// neither is something a competition arrives with - an importer can work out a match's round
    /// from the provider's stage but nothing reports its position in the draw, so the slots are
    /// numbered by an admin and are exactly the sort of thing that ends up with two 4s and no 5.
    ///
    /// Reported rather than merely detected because both failures look plausible from the outside.
    /// A bracket numbered wrongly still draws as a tree - just with the wrong ties in the wrong
    /// halves - and nothing downstream can tell. Left to a boolean, the only symptom an admin ever
    /// saw was the knockout view quietly declining to appear.
    /// </summary>
    /// <param name="rounds">
    /// The rounds of the bracket tree, largest first, with the slot each of their matches carries.
    /// The third-place play-off is no part of the tree and must not be included.
    /// </param>
    public static IReadOnlyList<string> DescribeProblems(IReadOnlyList<BracketRoundShape> rounds)
    {
        var problems = new List<string>();

        if (rounds.Count == 0)
        {
            problems.Add("No match has been given a bracket round yet.");
            return problems;
        }

        if (rounds[^1].KnockoutRound != 2)
        {
            problems.Add($"The bracket has no final - it stops at the {NameOf(rounds[^1].KnockoutRound)}.");
        }

        for (var index = 0; index < rounds.Count; index++)
        {
            var (knockoutRound, slots) = rounds[index];
            var name = NameOf(knockoutRound);

            // A round contested by n teams is n/2 matches. Safe as arithmetic here only because the
            // play-off's sentinel round is excluded by contract - see the parameter's remarks.
            var expectedMatches = knockoutRound / 2;

            // Each round halves the one before it, so a round whose predecessor is not twice its
            // size has one missing between them.
            if (index > 0 && rounds[index - 1].KnockoutRound != knockoutRound * 2)
            {
                problems.Add($"{NameOf(knockoutRound * 2)} is missing - {NameOf(rounds[index - 1].KnockoutRound)} is followed straight by {name}.");
            }

            if (slots.Count != expectedMatches)
            {
                problems.Add($"{name}: {slots.Count} {Matches(slots.Count)}, where this round needs {expectedMatches}.");
            }

            var unnumbered = slots.Count(slot => slot is null);
            if (unnumbered > 0)
            {
                problems.Add($"{name}: {unnumbered} {Matches(unnumbered)} with no bracket slot.");
            }

            var numbered = slots.Where(slot => slot.HasValue).Select(slot => slot!.Value).ToList();

            foreach (var duplicate in numbered.GroupBy(slot => slot).Where(group => group.Count() > 1).OrderBy(group => group.Key))
            {
                problems.Add($"{name}: slot {duplicate.Key} is used by {duplicate.Count()} matches.");
            }

            foreach (var outOfRange in numbered.Where(slot => slot < 1 || slot > expectedMatches).Distinct().Order())
            {
                problems.Add($"{name}: slot {outOfRange} is outside the range 1 to {expectedMatches}.");
            }

            // Which slots are absent is only worth naming once the round is the right size and every
            // match in it has been numbered. Before that it restates a count already reported above,
            // and a half-filled round would list every slot nobody has reached yet as a fault.
            if (unnumbered == 0 && slots.Count == expectedMatches)
            {
                foreach (var absent in Enumerable.Range(1, expectedMatches).Where(slot => !numbered.Contains(slot)))
                {
                    problems.Add($"{name}: no match is in slot {absent}.");
                }
            }
        }

        return problems;
    }

    private static string Matches(int count) => count == 1 ? "match" : "matches";
}

/// <summary>
/// One round of a bracket as <see cref="KnockoutRounds.DescribeProblems"/> reads it.
/// </summary>
/// <param name="KnockoutRound">The round's <c>KnockoutRound</c> value.</param>
/// <param name="Slots">
/// Each of the round's matches by its <c>BracketSlot</c>, in any order, null where a match has not
/// been given one.
/// </param>
public readonly record struct BracketRoundShape(int KnockoutRound, IReadOnlyList<int?> Slots);
