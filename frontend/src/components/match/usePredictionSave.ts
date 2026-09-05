import { useRef, useState } from "react";
import { ApiError } from "../../services/api";
import { savePrediction } from "../../services/prediction-service";
import type { SaveState } from "./matchStatus";

/// Saving one match's prediction, with the ordering guarantees score entry needs. Editing an
/// existing prediction fires a save per digit change (home, then away), so two POSTs can overlap.
/// Chaining them guarantees the server processes them in entry order (last write wins with the
/// *latest* values), and the sequence number keeps a superseded save's outcome from clobbering the
/// UI state of the one that matters.
///
/// Shared by every place a prediction can be entered - the Predictions page's rows and the Home
/// card's quick-predict popover - so they agree on what a 409 means (the save cutoff has passed,
/// not a general failure) as well as on the ordering.
export function usePredictionSave(matchId: string): {
    saveState: SaveState;
    /** No-op unless both sides have a value - half a scoreline isn't a prediction. */
    save: (homeValue: string, awayValue: string, onSaved?: () => void) => void;
} {
    const [saveState, setSaveState] = useState<SaveState>("idle");

    const saveChain = useRef<Promise<void>>(Promise.resolve());
    const saveSeq = useRef(0);

    const save = (homeValue: string, awayValue: string, onSaved?: () => void) => {
        if (homeValue === "" || awayValue === "") {
            return;
        }

        const seq = ++saveSeq.current;
        setSaveState("saving");

        saveChain.current = saveChain.current.then(async () => {
            try {
                await savePrediction(matchId, Number(homeValue), Number(awayValue));
                if (saveSeq.current !== seq) {
                    return;
                }

                setSaveState("saved");
                onSaved?.();
            } catch (error) {
                if (saveSeq.current !== seq) {
                    return;
                }

                setSaveState(error instanceof ApiError && error.status === 409 ? "cutoff" : "error");
            }
        });
    };

    return { saveState, save };
}
