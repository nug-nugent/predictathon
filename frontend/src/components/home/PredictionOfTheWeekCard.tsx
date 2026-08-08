import { Heading, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import { getCompetitionWeeks, computeDefaultWeek } from "../../services/prediction-service";
import { getBestPredictions, type BestPrediction } from "../../services/statistics-service";
import { weekEnd } from "../../utils/matchWeek";
import { Panel } from "../ui/panel";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../ui/async-state";

type State = { label: "This week's best prediction" | "Last week's best prediction"; entry: BestPrediction | null };

export function PredictionOfTheWeekCard({ competitionId }: { competitionId: string }) {
    const { data: state, error } = useAsyncData<State>(async () => {
        const weeks = await getCompetitionWeeks(competitionId);
        const currentWeek = computeDefaultWeek(weeks);
        const currentIndex = weeks.indexOf(currentWeek);
        const previousWeek = currentIndex > 0 ? weeks[currentIndex - 1] : null;

        const thisWeekBest = currentWeek ? await getBestPredictions(competitionId, currentWeek, weekEnd(currentWeek)) : [];
        if (thisWeekBest.length > 0) {
            return { label: "This week's best prediction", entry: thisWeekBest[0] };
        }

        const lastWeekBest = previousWeek ? await getBestPredictions(competitionId, previousWeek, weekEnd(previousWeek)) : [];
        return { label: "Last week's best prediction", entry: lastWeekBest[0] ?? null };
    }, [competitionId]);

    if (error) {
        return <ErrorState error={error} />;
    }

    if (state === null) {
        return <LoadingSpinner />;
    }

    const entry = state.entry;

    return (
        <Panel p={3}>
            <Heading fontSize="17px" fontWeight="semibold" mb={2}>{entry ? state.label : "Prediction of the Week"}</Heading>
            {entry === null ? (
                <Text color="fg.muted">No standout prediction yet this week.</Text>
            ) : (
                <VStack align="stretch" gap={1}>
                    <Text>
                        <RouterLink to={`/profile/${entry.userID}`}>{entry.username}</RouterLink> predicted{" "}
                        <Text as="span" fontWeight="bold">{entry.predictionHomeTeamGoals}-{entry.predictionAwayTeamGoals}</Text>
                        {" "}for {entry.homeTeamShortName} v {entry.awayTeamShortName}
                    </Text>
                    <Text fontSize="sm" color="fg.muted">
                        Final score {entry.homeTeamGoals}-{entry.awayTeamGoals} &middot; scored {entry.predictionScore} pts &middot; beat the average by {entry.scoreDifference.toFixed(2)}
                    </Text>
                </VStack>
            )}
        </Panel>
    );
}
