import { useEffect, useState } from "react";
import { Center, Spinner, Text, VStack } from "@chakra-ui/react";
import { getCompetitionWeeks, getMatchesForWeek, computeDefaultWeek, type MatchPrediction } from "../../services/prediction-service";
import { HomeMatchSummary } from "./HomeMatchSummary";
import { ApiError } from "../../services/api";
import { weekEnd } from "../../utils/matchWeek";

export function HomeMatchWeeksSection({ competitionId }: { competitionId: string }) {
    const [state, setState] = useState<{
        currentTitle: string;
        currentMatches: MatchPrediction[];
        futureMatches: MatchPrediction[] | null;
    } | null>(null);
    const [error, setError] = useState<ApiError | null>(null);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            try {
                const weeks = await getCompetitionWeeks(competitionId);
                const currentWeek = computeDefaultWeek(weeks);

                if (!currentWeek) {
                    if (!cancelled) setState({ currentTitle: "Current Matches", currentMatches: [], futureMatches: null });
                    return;
                }

                const currentIndex = weeks.indexOf(currentWeek);
                const futureWeek = currentIndex < weeks.length - 1 ? weeks[currentIndex + 1] : null;

                const [currentMatches, futureMatches] = await Promise.all([
                    getMatchesForWeek(competitionId, currentWeek),
                    futureWeek ? getMatchesForWeek(competitionId, futureWeek) : Promise.resolve(null),
                ]);

                if (cancelled) return;

                const currentWeekEnd = weekEnd(currentWeek);
                const currentTitle = new Date(currentWeekEnd) < new Date() ? "Last Matches" : "Current Matches";

                setState({ currentTitle, currentMatches, futureMatches });
            } catch (err) {
                if (!cancelled) setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."]));
            }
        };

        load();
        return () => { cancelled = true; };
    }, [competitionId]);

    if (error) {
        return (
            <Center py={4}>
                <Text>{error.messages.join(" ")}</Text>
            </Center>
        );
    }

    if (state === null) {
        return (
            <Center py={4}>
                <Spinner />
            </Center>
        );
    }

    return (
        <VStack align="stretch" gap={4}>
            <HomeMatchSummary title={state.currentTitle} matches={state.currentMatches} />
            {state.futureMatches !== null && <HomeMatchSummary title="Future Matches" matches={state.futureMatches} />}
        </VStack>
    );
}
