import { useCallback, useEffect, useMemo, useState } from "react";
import { useUser } from "../hooks/useUser";
import { getMyRegisteredCompetitions, type UserCompetitionRegistration } from "../services/competition-service";
import { CompetitionContext, type CompetitionContextType } from "../hooks/useCompetition";

const STORAGE_KEY = "currentCompetitionId";

// mockValue bypasses the real fetch entirely and renders children under a static context value -
// only ever set in Storybook (see .storybook/preview.tsx), so stories don't depend on a live
// backend. Left undefined (the real app), behaviour is unchanged.
export const CompetitionProvider = ({ mockValue, children }: { mockValue?: CompetitionContextType; children: React.ReactNode }) => {
    if (mockValue !== undefined) {
        return (
            <CompetitionContext.Provider value={mockValue}>
                {children}
            </CompetitionContext.Provider>
        );
    }

    return <LiveCompetitionProvider>{children}</LiveCompetitionProvider>;
};

// Always renders `children` under a single, stable CompetitionContext.Provider - the fetching
// logic below switches on `user`, but the element type/position never changes across a
// login/logout transition. That matters because `children` here is the whole routed page tree
// (see main.tsx); if this provider's own JSX shape changed across that transition, React would
// unmount and remount every page underneath it, silently discarding local component state (e.g.
// RegisterPage's in-progress step) at exactly the moment a page like Register logs a user in
// without navigating away.
const LiveCompetitionProvider = ({ children }: { children: React.ReactNode }) => {
    const { user, isLoading: userLoading } = useUser();

    const [competitions, setCompetitions] = useState<UserCompetitionRegistration[]>([]);
    const [currentCompetitionId, setCurrentCompetitionIdState] = useState<string | null>(null);
    // The logged-in identity (by id, matching the fetch effect's key) whose registrations the
    // state above currently reflects - set by the fetch effect's callbacks when a load settles.
    // isLoading is derived from it rather than stored, so the effect never has to set state
    // synchronously (react-hooks/set-state-in-effect).
    const [loadedForUser, setLoadedForUser] = useState<string | null>(null);

    // Re-fetches just the list, leaving currentCompetitionId alone - callers that just registered
    // for a new competition follow this with an explicit setCurrentCompetitionId to switch to it.
    // Deliberately doesn't read `user` - callers only ever invoke this right after a registration
    // action that itself required being logged in, and a stale closure over `user` (captured
    // before a same-tick setUser() has actually propagated through React) would make this silently
    // no-op right when it matters most.
    const refreshCompetitions = useCallback(async () => {
        const list = await getMyRegisteredCompetitions();
        setCompetitions(list);
    }, []);

    useEffect(() => {
        if (!user) {
            // No synchronous state resets here (react-hooks/set-state-in-effect) - while logged
            // out, the context value below is derived to empty regardless of what state holds,
            // and the next login's fetch overwrites it anyway. sessionStorage is an external
            // system, so clearing it does belong in an effect - but only on a genuine logout:
            // `user` is also null while UserProvider's silent session-refresh is still in flight
            // on initial load, which isn't a logout.
            if (!userLoading) {
                sessionStorage.removeItem(STORAGE_KEY);
            }
            return;
        }

        let cancelled = false;
        const userId = user.id;

        getMyRegisteredCompetitions()
            .then((list) => {
                if (cancelled) return;
                setCompetitions(list);

                const stored = sessionStorage.getItem(STORAGE_KEY);
                const storedStillValid = stored !== null && list.some((c) => c.competitionID === stored);
                const dbDefault = list.find((c) => c.isDefaultCompetition)?.competitionID;
                const resolved = storedStillValid ? stored : (dbDefault ?? list[0]?.competitionID ?? null);

                setCurrentCompetitionIdState(resolved);
                setLoadedForUser(userId);

                if (resolved) {
                    sessionStorage.setItem(STORAGE_KEY, resolved);
                } else {
                    sessionStorage.removeItem(STORAGE_KEY);
                }
            })
            .catch(() => {
                if (cancelled) return;
                setCompetitions([]);
                setCurrentCompetitionIdState(null);
                setLoadedForUser(userId);
            });

        return () => { cancelled = true; };
        // Re-fetches once per logged-in identity (login/logout/switch user), not on every render -
        // `user` is a fresh object each refresh cycle, so we key on the stable `id` instead
        // (not `name`, which the user can change on the profile edit page).
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [user?.id, userLoading]);

    const setCurrentCompetitionId = (competitionId: string) => {
        setCurrentCompetitionIdState(competitionId);
        sessionStorage.setItem(STORAGE_KEY, competitionId);
    };

    // On a genuine logout, forget which user the state was loaded for, so a later login (even of
    // the same account) starts with isLoading=true until its own fetch lands. Done as a guarded
    // render-time adjustment (React's "adjusting state when a prop changes" pattern) rather than
    // in the effect above, which must not set state synchronously.
    if (!user && !userLoading && loadedForUser !== null) {
        setLoadedForUser(null);
    }

    // While logged out, expose empty values rather than clearing state in the effect above - the
    // stale state from a previous login is never visible (a new login flips isLoading back on
    // until its own fetch lands, so consumers keep gating on it).
    const loggedIn = user !== null;
    const isLoading = userLoading || (loggedIn && loadedForUser !== user.id);
    const contextValue = useMemo(
        () => ({
            competitions: loggedIn ? competitions : [],
            currentCompetitionId: loggedIn ? currentCompetitionId : null,
            setCurrentCompetitionId,
            refreshCompetitions,
            isLoading,
        }),
        [loggedIn, competitions, currentCompetitionId, refreshCompetitions, isLoading]
    );

    return (
        <CompetitionContext.Provider value={contextValue}>
            {children}
        </CompetitionContext.Provider>
    );
};
