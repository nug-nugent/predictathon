import { useCallback, useEffect, useMemo, useState } from "react";
import { useUser } from "../hooks/useUser";
import { getMyRegisteredCompetitions, type UserCompetitionRegistration } from "../services/competition-service";
import { CompetitionContext } from "../hooks/useCompetition";

const STORAGE_KEY = "currentCompetitionId";

// Always renders `children` under a single, stable CompetitionContext.Provider - the fetching
// logic below switches on `user`, but the element type/position never changes across a
// login/logout transition. That matters because `children` here is the whole routed page tree
// (see main.tsx); if this provider's own JSX shape changed across that transition, React would
// unmount and remount every page underneath it, silently discarding local component state (e.g.
// RegisterPage's in-progress step) at exactly the moment a page like Register logs a user in
// without navigating away.
export const CompetitionProvider = ({ children }: { children: React.ReactNode }) => {
    const { user, isLoading: userLoading } = useUser();

    const [competitions, setCompetitions] = useState<UserCompetitionRegistration[]>([]);
    const [currentCompetitionId, setCurrentCompetitionIdState] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);

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
            setCompetitions([]);
            setCurrentCompetitionIdState(null);
            setIsLoading(false);

            // Only clear on a genuine logout - `user` also starts out null while UserProvider's
            // silent session-refresh is still in flight on initial load, which isn't a logout.
            if (!userLoading) {
                sessionStorage.removeItem(STORAGE_KEY);
            }
            return;
        }

        setIsLoading(true);
        getMyRegisteredCompetitions()
            .then((list) => {
                setCompetitions(list);

                const stored = sessionStorage.getItem(STORAGE_KEY);
                const storedStillValid = stored !== null && list.some((c) => c.competitionID === stored);
                const dbDefault = list.find((c) => c.isDefaultCompetition)?.competitionID;
                const resolved = storedStillValid ? stored : (dbDefault ?? list[0]?.competitionID ?? null);

                setCurrentCompetitionIdState(resolved);

                if (resolved) {
                    sessionStorage.setItem(STORAGE_KEY, resolved);
                } else {
                    sessionStorage.removeItem(STORAGE_KEY);
                }
            })
            .catch(() => {
                setCompetitions([]);
                setCurrentCompetitionIdState(null);
            })
            .finally(() => setIsLoading(false));
        // Re-fetches once per logged-in identity (login/logout/switch user), not on every render -
        // `user` is a fresh object each refresh cycle, so we key on the stable `name` instead.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [user?.name, userLoading]);

    const setCurrentCompetitionId = (competitionId: string) => {
        setCurrentCompetitionIdState(competitionId);
        sessionStorage.setItem(STORAGE_KEY, competitionId);
    };

    const contextValue = useMemo(
        () => ({
            competitions,
            currentCompetitionId,
            setCurrentCompetitionId,
            refreshCompetitions,
            isLoading,
        }),
        [competitions, currentCompetitionId, refreshCompetitions, isLoading]
    );

    return (
        <CompetitionContext.Provider value={contextValue}>
            {children}
        </CompetitionContext.Provider>
    );
};
