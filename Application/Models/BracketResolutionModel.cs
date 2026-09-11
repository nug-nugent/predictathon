using System.Text.Json.Serialization;

namespace Predictathon.Application.Models;

/// <summary>
/// Why a knockout slot is or isn't ready to be filled in.
/// </summary>
/// <remarks>
/// Serialised by name rather than by number, unlike the rest of this API. A status is read by a
/// person as often as by the client - it turns up in a network tab while working out why a slot
/// will not fill in - and "Waiting" says what 2 does not. Scoped to this enum deliberately:
/// switching the serializer over globally would change the wire format of every endpoint that
/// already ships one, for no benefit to any of them.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<BracketSlotStatus>))]
public enum BracketSlotStatus
{
    /// <summary>A team is already in the slot. Nothing to do, and nothing here will overwrite it.</summary>
    Settled,

    /// <summary>Everything the slot depends on has happened, and a team can be proposed.</summary>
    Ready,

    /// <summary>The group or the tie that settles this slot hasn't finished yet.</summary>
    Waiting,

    /// <summary>
    /// Nothing can be proposed and only an admin can say - an unreadable or absent placeholder, or
    /// a feeding tie that finished level, where who went through isn't in the score.
    /// </summary>
    Manual,
}

/// <summary>
/// A competition's undecided knockout slots and what each one is waiting on, as the bracket admin
/// page works through them. Read-only: nothing here changes a match until the admin picks slots and
/// posts them back.
/// </summary>
public class BracketResolutionModel
{
    /// <summary>The bracket's rounds, first round first, each with both sides of each of its ties.</summary>
    public IReadOnlyList<BracketResolutionRound> Rounds { get; set; } = [];

    /// <summary>How many slots could be filled in right now.</summary>
    public int ReadyCount { get; set; }

    /// <summary>How many slots still have nobody in them, ready or not.</summary>
    public int UnsettledCount { get; set; }
}

/// <summary>One round of the bracket, for the resolution screen.</summary>
public class BracketResolutionRound
{
    /// <summary>The round's <c>KnockoutRound</c> value - see <see cref="Common.KnockoutRounds"/>.</summary>
    public int KnockoutRound { get; set; }

    /// <summary>The round's display name, e.g. "Quarter-final".</summary>
    public string RoundName { get; set; } = "";

    /// <summary>The round's slots, in draw order, home side before away.</summary>
    public IReadOnlyList<BracketResolutionSlot> Slots { get; set; } = [];
}

/// <summary>One side of one knockout tie.</summary>
public class BracketResolutionSlot
{
    public Guid MatchID { get; set; }

    /// <summary>The tie this slot belongs to, by its description, e.g. "Quarter-final 1".</summary>
    public string MatchDescription { get; set; } = "";

    /// <summary>Which side of the tie - true for the home slot.</summary>
    public bool IsHome { get; set; }

    /// <summary>The slot's placeholder, e.g. "Winner Group A". Null where it has none.</summary>
    public string? Placeholder { get; set; }

    /// <summary>The team already in the slot, where there is one.</summary>
    public Guid? CurrentTeamID { get; set; }

    /// <summary>The name of the team already in the slot, where there is one.</summary>
    public string? CurrentTeamName { get; set; }

    /// <summary>The team the placeholder resolves to, where it resolves to one now.</summary>
    public Guid? ProposedTeamID { get; set; }

    /// <summary>The name of <see cref="ProposedTeamID"/>, for the screen to show.</summary>
    public string? ProposedTeamName { get; set; }

    /// <summary>Whether this slot can be filled in, and if not, why not.</summary>
    public BracketSlotStatus Status { get; set; }

    /// <summary>
    /// What the slot is waiting on, in words - "Group D: 2 matches left", "Semi-final 1 finished
    /// level". Empty where the slot is settled or ready, which need no explanation.
    /// </summary>
    public string Reason { get; set; } = "";
}

/// <summary>One slot an admin has chosen to fill, and the team to put in it.</summary>
public class BracketSlotAssignment
{
    public Guid MatchID { get; set; }

    /// <summary>Which side of the tie to fill - true for the home slot.</summary>
    public bool IsHome { get; set; }

    public Guid TeamID { get; set; }
}

/// <summary>What filling a batch of slots actually did.</summary>
public class BracketResolutionSummary
{
    /// <summary>How many slots were filled in.</summary>
    public int SlotsFilled { get; set; }
}
