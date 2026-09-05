import { Button, Heading, HStack, Link as ChakraLink, Popover, Portal, Stack, Text, VStack } from "@chakra-ui/react";
import { Star } from "lucide-react";
import { Link as RouterLink } from "react-router";
import { getCompetitionWeeks, computeDefaultWeek } from "../../services/prediction-service";
import { getBestPredictions, type BestPrediction } from "../../services/statistics-service";
import { weekEnd } from "../../utils/matchWeek";
import { Panel } from "../ui/panel";
import { IconChip } from "../ui/icon-chip";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../ui/async-state";

type State = { label: "Prediction of the Week" | "Last Week's Best Prediction"; entries: BestPrediction[] };

/**
 * The same scoreline on the same match can be predicted by several people, and they all beat the
 * average by the same amount - so the "best prediction" is a prediction, not a person. Everyone
 * who made the top-scoring one shares the card rather than one of them winning the tie arbitrarily.
 */
function tiedWithBest(entries: BestPrediction[]): BestPrediction[] {
    const best = entries[0];
    if (best === undefined) {
        return [];
    }

    return entries
        .filter((e) => e.matchID === best.matchID
            && e.predictionHomeTeamGoals === best.predictionHomeTeamGoals
            && e.predictionAwayTeamGoals === best.predictionAwayTeamGoals)
        // The stored procedure orders by score difference only, so tied rows come back in whatever
        // order the query happened to produce - sort by name so the list is stable between loads.
        .sort((a, b) => a.username.localeCompare(b.username));
}

export function PredictionOfTheWeekCard({ competitionId }: { competitionId: string }) {
    const { data: state, error } = useAsyncData<State>(async () => {
        const weeks = await getCompetitionWeeks(competitionId);
        const currentWeek = computeDefaultWeek(weeks);
        const currentIndex = weeks.indexOf(currentWeek);
        const previousWeek = currentIndex > 0 ? weeks[currentIndex - 1] : null;

        const thisWeekBest = currentWeek ? await getBestPredictions(competitionId, currentWeek, weekEnd(currentWeek)) : [];
        if (thisWeekBest.length > 0) {
            return { label: "Prediction of the Week", entries: tiedWithBest(thisWeekBest) };
        }

        const lastWeekBest = previousWeek ? await getBestPredictions(competitionId, previousWeek, weekEnd(previousWeek)) : [];
        return { label: "Last Week's Best Prediction", entries: tiedWithBest(lastWeekBest) };
    }, [competitionId]);

    if (error) {
        return <ErrorState error={error} />;
    }

    if (state === null) {
        return <LoadingSpinner />;
    }

    const entries = state.entries;
    const entry = entries[0] ?? null;

    return (
        <Panel p={3} accent hoverLift>
            <HStack gap={2} mb={2}>
                <IconChip icon={Star} color="points.3" />
                <Heading fontSize="17px" fontWeight="bold">{entry ? state.label : "Prediction of the Week"}</Heading>
            </HStack>
            {entry === null ? (
                <Text color="fg.muted">No standout prediction yet this week.</Text>
            ) : (
                <VStack align="stretch" gap={1}>
                    <Text>
                        {entries.length === 1 ? (
                            <RouterLink to={`/profile/${entry.userID}`}>{entry.username}</RouterLink>
                        ) : (
                            <Popover.Root positioning={{ placement: "bottom-start" }}>
                                <Popover.Trigger asChild>
                                    {/* Underlined rather than the plain profile links elsewhere on the card: it's the one
                                        thing here you can click, and nothing else hints that the names are behind it. */}
                                    <ChakraLink as="button" variant="underline" aria-label={`Show the ${entries.length} players who made this prediction`}>
                                        {entries.length} players
                                    </ChakraLink>
                                </Popover.Trigger>
                                <Portal>
                                    <Popover.Positioner>
                                        <Popover.Content width="auto" maxW="240px">
                                            <Popover.Arrow />
                                            <Popover.Body p={2}>
                                                <Stack gap={1} align="stretch">
                                                    {entries.map((e) => (
                                                        <Button key={e.userID} asChild size="xs" variant="ghost" justifyContent="flex-start">
                                                            <RouterLink to={`/profile/${e.userID}`}>{e.username}</RouterLink>
                                                        </Button>
                                                    ))}
                                                </Stack>
                                            </Popover.Body>
                                        </Popover.Content>
                                    </Popover.Positioner>
                                </Portal>
                            </Popover.Root>
                        )}
                        {" "}predicted{" "}
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
