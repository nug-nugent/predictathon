import { useEffect, useState, type DependencyList } from "react";
import { ApiError } from "../services/api";

// The standard page/widget data-loading pattern: fetch on mount (and whenever `deps` change),
// exposing { data, error, reload } for the usual error -> <ErrorState> / null -> <LoadingSpinner>
// render scaffold (see components/ui/async-state.tsx).
//
// - `reload()` clears any error (so the spinner shows) and re-runs the load. Existing data stays
//   visible until the new load settles, which also makes it suitable for refreshing after a
//   mutation (e.g. closing an admin edit dialog).
// - A load that loses a race to a newer one (deps changed, or reload called) is discarded.
// - Callers whose `deps` change without remounting keep showing the previous data while the new
//   load is in flight; key the component on the deps instead if a fresh spinner is wanted.
export function useAsyncData<T>(load: () => Promise<T>, deps: DependencyList): {
    data: T | null;
    error: ApiError | null;
    reload: () => void;
} {
    const [data, setData] = useState<T | null>(null);
    const [error, setError] = useState<ApiError | null>(null);
    const [attempt, setAttempt] = useState(0);

    useEffect(() => {
        let cancelled = false;

        load()
            .then((result) => {
                if (cancelled) return;
                setData(result);
                setError(null);
            })
            .catch((err) => {
                if (cancelled) return;
                setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."]));
            });

        return () => { cancelled = true; };
        // `load` is intentionally not a dependency - callers pass fresh closures every render,
        // and re-running on every render would loop. `deps` declares what the load depends on.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [...deps, attempt]);

    return {
        data,
        error,
        reload: () => {
            setError(null);
            setAttempt((a) => a + 1);
        },
    };
}
