import type { MatchPrediction } from "../../services/prediction-service";

export type MatchStatusValue = "Pre" | "During" | "Post";

export type SaveState = "idle" | "saving" | "saved" | "error" | "cutoff";

export type ComputedMatchStatus = {
    status: MatchStatusValue;
    minutesToPredict: number;
};

// Kept in lockstep with the server's 2-minute save cutoff (PredictionService.SavePredictionAsync) -
// showing "Pre"/editable any longer than the server actually allows would mean saves silently fail
// right as a match approaches kickoff. Exported so other prediction-deadline UI (e.g. the Home
// page's PredictionDeadlineCard) shares this one source of truth instead of hardcoding its own.
export const CUTOFF_MINUTES = 2;

export function computeMatchStatus(match: MatchPrediction, now: Date): ComputedMatchStatus {
    const kickoff = new Date(match.matchDateTime);
    const minutesToPredict = Math.ceil((kickoff.getTime() - (now.getTime() + CUTOFF_MINUTES * 60000)) / 60000);

    const status: MatchStatusValue = minutesToPredict < 1
        ? (match.matchPlayed ? "Post" : "During")
        : "Pre";

    return { status, minutesToPredict };
}
