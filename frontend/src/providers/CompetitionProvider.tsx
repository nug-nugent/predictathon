import { useEffect, useMemo, useState } from "react";
import { useUser } from "../hooks/useUser";
import { getMyRegisteredCompetitions, type UserCompetitionRegistration } from "../services/competition-service";
import { CompetitionContext, defaultCompetitionContextValue } from "../hooks/useCompetition";

const STORAGE_KEY = "currentCompetitionId";

// Fetches and holds registered-competition state for a single logged-in user. Mounted keyed by
// username, so logging in as a different person in the same tab remounts this fresh rather than
// needing to explicitly reset state left over from the previous session.
const LoggedInCompetitionProvider = ({ children }: { children: React.ReactNode }) => {
    const [competitions, setCompetitions] = useState<UserCompetitionRegistration[]>([]);
    const [currentCompetitionId, setCurrentCompetitionIdState] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
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
        // Deliberately runs once per mount (i.e. once per logged-in user - see the `key` this is
        // mounted with in CompetitionProvider below).
    }, []);

    const setCurrentCompetitionId = (competitionId: string) => {
        setCurrentCompetitionIdState(competitionId);
        sessionStorage.setItem(STORAGE_KEY, competitionId);
    };

    const contextValue = useMemo(
        () => ({
            competitions,
            currentCompetitionId,
            setCurrentCompetitionId,
            isLoading,
        }),
        [competitions, currentCompetitionId, isLoading]
    );

    return (
        <CompetitionContext.Provider value={contextValue}>
            {children}
        </CompetitionContext.Provider>
    );
};

export const CompetitionProvider = ({ children }: { children: React.ReactNode }) => {
    const { user, isLoading: userLoading } = useUser();

    useEffect(() => {
        // Only clear on a genuine logout - `user` also starts out null while UserProvider's
        // silent session-refresh is still in flight on initial load, which isn't a logout.
        if (!user && !userLoading) {
            sessionStorage.removeItem(STORAGE_KEY);
        }
    }, [user, userLoading]);

    if (!user) {
        return (
            <CompetitionContext.Provider value={defaultCompetitionContextValue}>
                {children}
            </CompetitionContext.Provider>
        );
    }

    return (
        <LoggedInCompetitionProvider key={user.name}>
            {children}
        </LoggedInCompetitionProvider>
    );
};
