import { getJsonAuthenticated, postJsonAuthenticated } from "./api";

/// Why a knockout slot is or isn't ready to be filled in.
/// Matches Application/Models/BracketResolutionModel.cs.
export type BracketSlotStatus = "Settled" | "Ready" | "Waiting" | "Manual";

export type BracketResolutionSlot = {
    matchID: string;
    /// The tie this slot belongs to, by its description, e.g. "Quarter-final 1".
    matchDescription: string;
    /// Which side of the tie - true for the home slot.
    isHome: boolean;
    /// The slot's placeholder, e.g. "Winner Group A". Null where it has none.
    placeholder: string | null;
    currentTeamID: string | null;
    currentTeamName: string | null;
    /// The team the placeholder resolves to, where it resolves to one now.
    proposedTeamID: string | null;
    proposedTeamName: string | null;
    status: BracketSlotStatus;
    /// What the slot is waiting on, in words. Empty where it's settled or ready.
    reason: string;
};

export type BracketResolutionRound = {
    knockoutRound: number;
    roundName: string;
    /// The round's slots in draw order, home side before away.
    slots: BracketResolutionSlot[];
};

export type BracketResolution = {
    rounds: BracketResolutionRound[];
    /// How many slots could be filled in right now.
    readyCount: number;
    /// How many slots still have nobody in them, ready or not.
    unsettledCount: number;
};

export type BracketSlotAssignment = {
    matchID: string;
    isHome: boolean;
    teamID: string;
};

export type BracketResolutionSummary = {
    slotsFilled: number;
};

/// Every slot in a competition's bracket and what each is waiting on. Changes nothing.
export async function getBracketResolution(competitionId: string): Promise<BracketResolution> {
    return getJsonAuthenticated<BracketResolution>(`/Match/${competitionId}/Bracket/Resolution`);
}

/// Fills in the picked slots. A slot that already holds a team is left alone, so applying a screen
/// that has gone stale can't overwrite a correction made in the meantime.
export async function resolveBracketSlots(
    competitionId: string,
    assignments: BracketSlotAssignment[],
): Promise<BracketResolutionSummary> {
    return postJsonAuthenticated<BracketResolutionSummary>(
        `/Match/${competitionId}/Bracket/Resolution`, assignments);
}
