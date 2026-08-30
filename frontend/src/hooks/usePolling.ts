import { useEffect, useRef } from "react";

/// Calls `callback` every `intervalMs` while the component is mounted and the tab is visible.
/// Used to keep live match data fresh without a push channel - the callback is held in a ref so
/// callers can pass a fresh closure each render (e.g. useAsyncData's `reload`) without restarting
/// the timer. Polling pauses while the tab is hidden and fires once immediately on return, so a
/// page left open in a background tab doesn't keep hitting the API all afternoon.
export function usePolling(callback: () => void, intervalMs: number) {
    const savedCallback = useRef(callback);

    // Assigned in an effect rather than during render - a ref write during render is both a lint
    // error here and genuinely unsafe under concurrent rendering.
    useEffect(() => {
        savedCallback.current = callback;
    });

    useEffect(() => {
        let timer: ReturnType<typeof setInterval> | undefined;

        const stop = () => {
            clearInterval(timer);
            timer = undefined;
        };

        const start = () => {
            if (timer !== undefined) return;
            timer = setInterval(() => savedCallback.current(), intervalMs);
        };

        const onVisibilityChange = () => {
            if (document.hidden) {
                stop();
                return;
            }

            savedCallback.current();
            start();
        };

        start();
        document.addEventListener("visibilitychange", onVisibilityChange);

        return () => {
            stop();
            document.removeEventListener("visibilitychange", onVisibilityChange);
        };
    }, [intervalMs]);
}
