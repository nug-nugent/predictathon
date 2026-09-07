namespace Predictathon.Application.Models;

/// <summary>
/// A competition's knockout stage, arranged as the bracket view draws it: the rounds of the tree
/// from the first through to the final, plus the third-place play-off, which is a knockout match
/// but no part of the tree and so is handed over separately rather than as a round of its own.
/// </summary>
public class KnockoutBracketModel
{
    /// <summary>
    /// The rounds of the bracket tree, first round first (the round of 16 before the quarter-finals,
    /// and so on). Empty where the competition has no bracket.
    /// </summary>
    public IReadOnlyList<KnockoutRoundModel> Rounds { get; set; } = [];

    /// <summary>The third-place play-off, or null where the competition doesn't have one.</summary>
    public UserMatchPredictionListItem? ThirdPlacePlayOff { get; set; }

    /// <summary>
    /// Whether <see cref="Rounds"/> forms a complete single-elimination tree - each round half the
    /// size of the one before it, every match present, ending at the final. False where a bracket
    /// is half-numbered or otherwise incomplete, which is the view's cue to fall back to the plain
    /// match list rather than draw a tree full of holes.
    /// </summary>
    public bool IsWellFormed { get; set; }
}

/// <summary>
/// One round of a knockout bracket, with the current user's predictions against each of its matches.
/// </summary>
public class KnockoutRoundModel
{
    /// <summary>
    /// The round's <c>KnockoutRound</c> value - see <see cref="Common.KnockoutRounds"/>. An ordering
    /// key, not a number to calculate with.
    /// </summary>
    public int KnockoutRound { get; set; }

    /// <summary>The round's display name, e.g. "Quarter-final".</summary>
    public string RoundName { get; set; } = "";

    /// <summary>The round's matches in draw order, by BracketSlot.</summary>
    public IReadOnlyList<UserMatchPredictionListItem> Matches { get; set; } = [];
}
