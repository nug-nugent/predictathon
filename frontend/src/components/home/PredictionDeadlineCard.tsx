import { Button, Heading, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import { getCompetitionWeeks, computeDefaultWeek, getMatchesForWeek } from "../../services/prediction-service";
import { Panel } from "../ui/panel";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../ui/async-state";
import { formatCountdown, countdownColor } from "../../utils/countdown";
import { CUTOFF_MINUTES } from "../match/matchStatus";

type NextDeadline = { homeTeamShortName: string; awayTeamShortName: string; deadline: Date; remaining: number };

// Wrapped in an object (rather than `NextDeadline | null` directly) so useAsyncData's "no data yet"
// sentinel (null) stays distinguishable from "loaded, and there's nothing left to predict" (also
// semantically null) - see UserStatisticsCard's Stats type for the same pattern.
type State = { next: NextDeadline | null };

export function PredictionDeadlineCard({ competitionId }: { competitionId: string }) {
    const { data: state, error } = useAsyncData<State>(async () => {
        const weeks = await getCompetitionWeeks(competitionId);
        const currentWeek = computeDefaultWeek(weeks);
        if (!currentWeek) return { next: null };

        const matches = await getMatchesForWeek(competitionId, currentWeek);
        const now = new Date();
        // Only matches whose deadline hasn't passed yet are still predictable - a match with no
        // prediction but a past deadline (e.g. an undecided knockout placeholder whose kick-off time
        // came and went) is missed, not "next up".
        const predictable = matches
            .filter((m) => m.predictionID === null)
            .map((m) => ({ ...m, deadline: new Date(new Date(m.matchDateTime).getTime() - CUTOFF_MINUTES * 60000) }))
            .filter((m) => m.deadline > now)
            .sort((a, b) => a.deadline.getTime() - b.deadline.getTime());

        if (predictable.length === 0) return { next: null };

        const soonest = predictable[0];

        return { next: { homeTeamShortName: soonest.homeTeamShortName, awayTeamShortName: soonest.awayTeamShortName, deadline: soonest.deadline, remaining: predictable.length } };
    }, [competitionId]);

    if (error) {
        return <ErrorState error={error} />;
    }

    if (state === null) {
        return <LoadingSpinner />;
    }

    const next = state.next;
    const now = new Date();

    return (
        <Panel p={3}>
            <Heading fontSize="17px" fontWeight="semibold" mb={2}>Prediction Deadline</Heading>
            {next === null ? (
                <Text color="green.500">All matches predicted!</Text>
            ) : (
                <VStack align="stretch" gap={1}>
                    <Text>
                        {next.homeTeamShortName} v {next.awayTeamShortName} closes in{" "}
                        <Text as="span" fontWeight="bold" color={countdownColor(next.deadline, now)}>
                            {formatCountdown(next.deadline, now)}
                        </Text>
                    </Text>
                    {next.remaining > 1 && (
                        <Text fontSize="sm" color="fg.muted">+{next.remaining - 1} more to predict this week</Text>
                    )}
                    <Button asChild size="xs" variant="ghost" alignSelf="flex-start" mt={1}>
                        <RouterLink to="/predictions">Predict now</RouterLink>
                    </Button>
                </VStack>
            )}
        </Panel>
    );
}
