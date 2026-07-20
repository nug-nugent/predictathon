import { VStack } from "@chakra-ui/react";
import { getCompetitionWeeks, getMatchesForWeek, computeDefaultWeek, type MatchPrediction } from "../../services/prediction-service";
import { HomeMatchSummary } from "./HomeMatchSummary";
import { weekEnd } from "../../utils/matchWeek";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../components/ui/async-state";

type WeeksSummary = {
    currentTitle: string;
    currentMatches: MatchPrediction[];
    futureMatches: MatchPrediction[] | null;
};

export function HomeMatchWeeksSection({ competitionId }: { competitionId: string }) {
    const { data: state, error } = useAsyncData<WeeksSummary>(async () => {
        const weeks = await getCompetitionWeeks(competitionId);
        const currentWeek = computeDefaultWeek(weeks);

        if (!currentWeek) {
            return { currentTitle: "Current Matches", currentMatches: [], futureMatches: null };
        }

        const currentIndex = weeks.indexOf(currentWeek);
        const futureWeek = currentIndex < weeks.length - 1 ? weeks[currentIndex + 1] : null;

        const [currentMatches, futureMatches] = await Promise.all([
            getMatchesForWeek(competitionId, currentWeek),
            futureWeek ? getMatchesForWeek(competitionId, futureWeek) : Promise.resolve(null),
        ]);

        const currentTitle = new Date(weekEnd(currentWeek)) < new Date() ? "Last Matches" : "Current Matches";

        return { currentTitle, currentMatches, futureMatches };
    }, [competitionId]);

    if (error) {
        return <ErrorState error={error} />;
    }

    if (state === null) {
        return <LoadingSpinner />;
    }

    return (
        <VStack align="stretch" gap={4}>
            <HomeMatchSummary title={state.currentTitle} matches={state.currentMatches} />
            {state.futureMatches !== null && <HomeMatchSummary title="Future Matches" matches={state.futureMatches} />}
        </VStack>
    );
}
