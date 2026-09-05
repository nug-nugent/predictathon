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

// Just the duration - the caller supplies the "closes in" framing. Naming the deadline matters now
// that MatchList bands each match under its kick-off time: a bare "5h" next to a 15:00 heading
// invites reading it as the time to kick-off rather than to the (two minutes earlier) save cutoff.
export function formatCountdown(minutes: number): string {
    if (minutes < 60) {
        return `${minutes}m`;
    }

    const hours = Math.floor(minutes / 60);
    if (hours < 24) {
        const remainingMinutes = minutes % 60;
        return `${hours}h${remainingMinutes ? ` ${remainingMinutes}m` : ""}`;
    }

    const days = Math.floor(hours / 24);
    const remainingHours = hours % 24;
    return `${days}d${remainingHours ? ` ${remainingHours}h` : ""}`;
}

/// The colour the deadline/save line reads in. Shared so every place a prediction can be entered -
/// the Predictions page's rows and the Home card's quick-predict popover - reports a save, a
/// failure and a closing deadline the same way.
export function predictionStatusColor(status: MatchStatusValue, saveState: SaveState, minutesToPredict: number): string {
    if (status !== "Pre") return "fg.muted";
    if (saveState === "cutoff" || saveState === "error") return "fg.error";
    if (saveState === "saving") return "fg.info";
    if (saveState === "saved") return "fg.success";

    return minutesToPredict < 1440 ? "status.urgent" : "status.relaxed";
}

/// The wording that goes with predictionStatusColor - see that function for why it's shared.
export function predictionStatusText(status: MatchStatusValue, saveState: SaveState, minutesToPredict: number): string {
    if (status !== "Pre") return "Awaiting result";
    if (saveState === "cutoff") return "Predictions are closed for this match";
    if (saveState === "error") return "Failed to save prediction!";
    if (saveState === "saving") return "Saving...";
    if (saveState === "saved") return "Prediction saved!";

    return `Closes in ${formatCountdown(minutesToPredict)}`;
}
