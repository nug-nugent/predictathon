import { Heading, HStack, Link, Text, VStack } from "@chakra-ui/react";
import { Timer } from "lucide-react";
import { Link as RouterLink } from "react-router";
import { getCompetitionWeekSummaries, computePredictionsLandingWeek, getMatchesForWeek } from "../../services/prediction-service";
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
        const summaries = await getCompetitionWeekSummaries(competitionId);
        const now = new Date();

        // The server already knows which weeks hold predictions this user can still make, so go
        // straight to the earliest of them - this used to walk the season a week at a time, which
        // got slower with every round played.
        const outstandingWeek = summaries.find((s) => s.openUnpredictedCount > 0);
        if (!outstandingWeek) return { next: null, upcoming: null };

        const matches = await getMatchesForWeek(competitionId, outstandingWeek.weekStart);
        const predictable = predictableMatches(matches, now);

        // The count comes from the server clock, which still counts a match as open through the two
        // minutes before kick-off that the client has already closed. Nothing actionable to show.
        if (predictable.length === 0) return { next: null, upcoming: null };

        // Counting down only makes sense for the week /predictions itself opens on; anything
        // further out is a heads-up that links to the week in question.
        const soonest = predictable[0];
        if (outstandingWeek.weekStart !== computePredictionsLandingWeek(summaries, now)) {
            return { next: null, upcoming: { week: outstandingWeek.weekStart, deadline: soonest.deadline } };
        }

        return { next: { homeTeamShortName: soonest.homeTeamShortName, awayTeamShortName: soonest.awayTeamShortName, deadline: soonest.deadline, remaining: predictable.length }, upcoming: null };
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
                    <Text color="green.500">All your predictions are in.</Text>
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
                        <Text fontSize="sm" color="fg.muted">+{next.remaining - 1} more to predict</Text>
                    )}
                    <Link asChild colorPalette="action" fontSize="14px" fontWeight="bold" alignSelf="flex-start" mt={1}>
                        <RouterLink to="/predictions">Predict now &rarr;</RouterLink>
                    </Link>
                </VStack>
            )}
        </Panel>
    );
}
