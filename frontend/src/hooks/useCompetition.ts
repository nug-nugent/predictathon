import { createContext, useContext } from "react";
import type { UserCompetitionRegistration } from "../services/competition-service";

type CompetitionContextType = {
    competitions: UserCompetitionRegistration[];
    currentCompetitionId: string | null;
    setCurrentCompetitionId: (competitionId: string) => void;
    isLoading: boolean;
};

export const defaultCompetitionContextValue: CompetitionContextType = {
    competitions: [],
    currentCompetitionId: null,
    setCurrentCompetitionId: () => { },
    isLoading: false,
};

export const CompetitionContext = createContext<CompetitionContextType>(defaultCompetitionContextValue);

export const useCompetition = () => useContext(CompetitionContext);
