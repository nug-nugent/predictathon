import { Heading, HStack, Link, Text, VStack } from "@chakra-ui/react";
import { Timer } from "lucide-react";
import { Link as RouterLink } from "react-router";
import { getCompetitionWeeks, computeDefaultWeek, getMatchesForWeek } from "../../services/prediction-service";
import { Panel } from "../ui/panel";
import { IconChip } from "../ui/icon-chip";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../ui/async-state";
import { formatCountdown, countdownColor } from "../../utils/countdown";
import { CUTOFF_MINUTES } from "../match/matchStatus";

type NextDeadline = { homeTeamShortName: string; awayTeamShortName: string; deadline: Date; remaining: number };
type UpcomingWeek = { week: string; deadline: Date };

// Wrapped in an object (rather than `NextDeadline | null` directly) so useAsyncData's "no data yet"
// sentinel (null) stays distinguishable from "loaded, and there's nothing left to predict" (also
// semantically null) - see UserStatisticsCard's Stats type for the same pattern.
type State = { next: NextDeadline | null; upcoming: UpcomingWeek | null };

// Only matches whose deadline hasn't passed yet are still predictable - a match with no prediction
// but a past deadline (e.g. an undecided knockout placeholder whose kick-off time came and went) is
// missed, not "next up".
function predictableMatches(matches: Awaited<ReturnType<typeof getMatchesForWeek>>, now: Date) {
    return matches
        .filter((m) => m.predictionID === null)
        .map((m) => ({ ...m, deadline: new Date(new Date(m.matchDateTime).getTime() - CUTOFF_MINUTES * 60000) }))
        .filter((m) => m.deadline > now)
        .sort((a, b) => a.deadline.getTime() - b.deadline.getTime());
}

function formatDeadline(deadline: Date): string {
    return deadline.toLocaleDateString(undefined, { dateStyle: "medium" });
}

export function PredictionDeadlineCard({ competitionId }: { competitionId: string }) {
    const { data: state, error } = useAsyncData<State>(async () => {
        const weeks = await getCompetitionWeeks(competitionId);
        const currentWeek = computeDefaultWeek(weeks);
        if (!currentWeek) return { next: null, upcoming: null };

        const now = new Date();
        const matches = await getMatchesForWeek(competitionId, currentWeek);
        const predictable = predictableMatches(matches, now);

        if (predictable.length > 0) {
            const soonest = predictable[0];
            return { next: { homeTeamShortName: soonest.homeTeamShortName, awayTeamShortName: soonest.awayTeamShortName, deadline: soonest.deadline, remaining: predictable.length }, upcoming: null };
        }

        // This week's fully predicted (or missed) - look ahead week by week for whenever the next
        // prediction opportunity actually closes, so the card still has something useful to say.
        const futureWeeks = weeks.filter((w) => new Date(w) > new Date(currentWeek));
        for (const week of futureWeeks) {
            const weekMatches = await getMatchesForWeek(competitionId, week);
            const weekPredictable = predictableMatches(weekMatches, now);
            if (weekPredictable.length > 0) {
                return { next: null, upcoming: { week, deadline: weekPredictable[0].deadline } };
            }
        }

        return { next: null, upcoming: null };
    }, [competitionId]);

    if (error) {
        return <ErrorState error={error} />;
    }

    if (state === null) {
        return <LoadingSpinner />;
    }

    const { next, upcoming } = state;
    const now = new Date();

    return (
        <Panel p={3} accent hoverLift>
            <HStack gap={2} mb={2}>
                <IconChip icon={Timer} color="status.urgent" />
                <Heading fontSize="17px" fontWeight="bold">Prediction Deadline</Heading>
            </HStack>
            {next === null ? (
                <VStack align="stretch" gap={1}>
                    <Text color="green.500">All matches this week predicted.</Text>
                    {upcoming && (
                        <Link asChild colorPalette="action" fontSize="14px" fontWeight="bold" alignSelf="flex-start" mt={1}>
                            <RouterLink to={`/predictions?week=${encodeURIComponent(upcoming.week)}`}>
                                Next prediction due: {formatDeadline(upcoming.deadline)} &rarr;
                            </RouterLink>
                        </Link>
                    )}
                </VStack>
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
                    <Link asChild colorPalette="action" fontSize="14px" fontWeight="bold" alignSelf="flex-start" mt={1}>
                        <RouterLink to="/predictions">Predict now &rarr;</RouterLink>
                    </Link>
                </VStack>
            )}
        </Panel>
    );
}
