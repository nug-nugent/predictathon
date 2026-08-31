import { createContext, useContext } from "react";
import type { UserCompetitionRegistration } from "../services/competition-service";

export type CompetitionContextType = {
    competitions: UserCompetitionRegistration[];
    currentCompetitionId: string | null;
    setCurrentCompetitionId: (competitionId: string) => void;
    // Re-fetches the competitions list without touching currentCompetitionId - call after
    // registering for a new competition so it shows up (e.g. in the header's CompetitionSelector)
    // without waiting for the next login.
    refreshCompetitions: () => Promise<void>;
    isLoading: boolean;
};

const defaultCompetitionContextValue: CompetitionContextType = {
    competitions: [],
    currentCompetitionId: null,
    setCurrentCompetitionId: () => { },
    refreshCompetitions: async () => { },
    isLoading: false,
};

export const CompetitionContext = createContext<CompetitionContextType>(defaultCompetitionContextValue);

export const useCompetition = () => useContext(CompetitionContext);
