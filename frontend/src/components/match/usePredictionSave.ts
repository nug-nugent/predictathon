import { useCallback, useEffect, useRef, useState } from "react";
import { ApiError } from "../../services/api";
import { savePrediction } from "../../services/prediction-service";
import type { SaveState } from "./matchStatus";

/// How long to wait after the last keystroke before sending. Editing an existing prediction means
/// changing two digits in quick succession, and each one on its own is a complete scoreline the
/// user never chose - "3 - <old away>" on the way to "3 - 1". Sending those intermediate pairs
/// wrote a row to PredictionHistory that the results reconciliation can revert to, and left a
/// window near the cutoff where the first digit was accepted and the second refused, storing half
/// the old prediction and half the new one. Long enough to coalesce a two-digit edit, short enough
/// that "Saving..." doesn't linger if you stop after one.
const SAVE_DEBOUNCE_MS = 500;

type SaveAttempt = {
    home: number;
    away: number;
    onSaved?: () => void;
};

/// Saving one match's prediction, with the ordering guarantees score entry needs. Saves are
/// debounced so a home-then-away edit becomes a single POST, then chained so that any that do
/// overlap (a slow connection outrunning the debounce) are processed in entry order - last write
/// wins with the *latest* values - with the sequence number keeping a superseded save's outcome
/// from clobbering the UI state of the one that matters.
///
/// Because the debounce creates a window where an edit is on screen but not yet sent, every way out
/// of that window has to flush it: `flush` is called when focus leaves the row, when the popover
/// closes, on unmount, and when the page is hidden or closed (see the pagehide effect below).
///
/// Shared by every place a prediction can be entered - the Predictions page's rows and the Home
/// card's quick-predict popover - so they agree on what a 409 means (the save cutoff has passed,
/// not a general failure) as well as on the ordering.
export function usePredictionSave(matchId: string): {
    saveState: SaveState;
    /**
     * Queues a save for `SAVE_DEBOUNCE_MS`. No-op unless both sides have a value - half a scoreline
     * isn't a prediction - and a half-entered pair also cancels any save still queued, so an
     * abandoned edit leaves the stored prediction alone rather than committing a pair the user was
     * partway through replacing.
     */
    save: (homeValue: string, awayValue: string, onSaved?: () => void) => void;
    /** Sends a queued save now, rather than waiting out the debounce. No-op if nothing is queued. */
    flush: (options?: { keepalive?: boolean }) => void;
    /** Re-sends the last attempt, for recovering from a failed save without retyping. */
    retry: () => void;
} {
    const [saveState, setSaveState] = useState<SaveState>("idle");

    const saveChain = useRef<Promise<void>>(Promise.resolve());
    const saveSeq = useRef(0);
    const pending = useRef<SaveAttempt | null>(null);
    const lastAttempt = useRef<SaveAttempt | null>(null);
    const timer = useRef<number | null>(null);

    const clearTimer = () => {
        if (timer.current !== null) {
            window.clearTimeout(timer.current);
            timer.current = null;
        }
    };

    const send = useCallback((attempt: SaveAttempt, options?: { keepalive?: boolean }) => {
        lastAttempt.current = attempt;

        const seq = ++saveSeq.current;
        setSaveState("saving");

        saveChain.current = saveChain.current.then(async () => {
            try {
                await savePrediction(matchId, attempt.home, attempt.away, options);
                if (saveSeq.current !== seq) {
                    return;
                }

                setSaveState("saved");
                attempt.onSaved?.();
            } catch (error) {
                if (saveSeq.current !== seq) {
                    return;
                }

                setSaveState(error instanceof ApiError && error.status === 409 ? "cutoff" : "error");
            }
        });
    }, [matchId]);

    const flush = useCallback((options?: { keepalive?: boolean }) => {
        clearTimer();

        const attempt = pending.current;
        if (attempt === null) {
            return;
        }

        pending.current = null;
        send(attempt, options);
    }, [send]);

    const save = (homeValue: string, awayValue: string, onSaved?: () => void) => {
        clearTimer();

        if (homeValue === "" || awayValue === "") {
            pending.current = null;
            setSaveState("idle");
            return;
        }

        pending.current = { home: Number(homeValue), away: Number(awayValue), onSaved };

        // Reported as "Saving..." from the keystroke rather than from the send, so the debounce
        // doesn't read as unresponsiveness.
        setSaveState("saving");
        timer.current = window.setTimeout(() => {
            timer.current = null;
            flush();
        }, SAVE_DEBOUNCE_MS);
    };

    const retry = () => {
        if (lastAttempt.current !== null) {
            send(lastAttempt.current);
        }
    };

    // A pending save has to survive the page going away - a plain fetch started here would be
    // cancelled, losing the edit. pagehide covers closing and navigating away; visibilitychange is
    // the one that fires on mobile when the browser is backgrounded, which is where most of these
    // predictions get entered in the first place.
    useEffect(() => {
        const flushForUnload = () => flush({ keepalive: true });
        const flushIfHidden = () => {
            if (document.visibilityState === "hidden") {
                flushForUnload();
            }
        };

        window.addEventListener("pagehide", flushForUnload);
        document.addEventListener("visibilitychange", flushIfHidden);

        return () => {
            window.removeEventListener("pagehide", flushForUnload);
            document.removeEventListener("visibilitychange", flushIfHidden);

            // Unmounting (changing week, leaving the page) is the other way out of the debounce
            // window. No keepalive: the app is still running, so an ordinary request completes.
            flush();
        };
    }, [flush]);

    return { saveState, save, flush, retry };
}
