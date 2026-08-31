import { Box, Button, Heading, HStack, Link as ChakraLink, SimpleGrid, Text, VStack } from "@chakra-ui/react";
import { Radio } from "lucide-react";
import { Link as RouterLink } from "react-router";
import { getTodaysMatches } from "../../services/match-service";
import type { MatchPrediction } from "../../services/prediction-service";
import { groupLiveMatches, hasLiveDayMatches } from "../../utils/liveMatches";
import { useAsyncData } from "../../hooks/useAsyncData";
import { useMinuteTick } from "../../hooks/useMinuteTick";
import { usePolling } from "../../hooks/usePolling";
import { LiveMatchRow } from "../match/live-match-row/LiveMatchRow";
import { LiveBadge, LivePulseDot } from "../match/live-badge/LiveBadge";
import { Panel } from "../ui/panel";
import { IconChip } from "../ui/icon-chip";
import { ErrorState } from "../ui/async-state";

// A minute is plenty: the only things that change under this card are an admin confirming a result
// and matches crossing their kick-off, and useMinuteTick already re-buckets the latter for free.
const REFRESH_MS = 60000;

/// Today's matches, split into what's still to come, what's in play, and what's finished. Renders
/// nothing at all on days the competition has no matches, which is most of them - the card only
/// earns its place at the top of the Home page on the days it has something to say.
export function TodaysMatchesCard({ competitionId }: { competitionId: string }) {
    const now = useMinuteTick();
    const { data: matches, error, reload } = useAsyncData(() => getTodaysMatches(competitionId), [competitionId]);

    usePolling(reload, REFRESH_MS);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    // No spinner while the first load is in flight: on most days this section turns out to be
    // absent entirely, and a spinner that resolves to nothing is worse than a moment of nothing.
    if (matches === null) {
        return null;
    }

    const groups = groupLiveMatches(matches, now);
    if (!hasLiveDayMatches(groups)) {
        return null;
    }

    const isLive = groups.live.length > 0;

    return (
        <Panel p={3} accent mb={3}>
            <HStack gap={2} mb={3} justify="space-between">
                <HStack gap={2}>
                    <IconChip icon={Radio} color={isLive ? "status.live" : "brand.accent"} />
                    <Heading fontSize="17px" fontWeight="bold">Today's Matches</Heading>
                </HStack>

                {/* With something in play the pulsing badge doubles as the way in to the Live page,
                    so the thing drawing the eye is the thing you can click. With nothing in play
                    there's nothing to draw the eye to, so it steps back to the same quiet ghost
                    button the profile card uses for "Edit User". */}
                {isLive ? (
                    <ChakraLink asChild variant="underline" fontSize="sm" fontWeight="bold" flexShrink={0}
                        color="status.live">
                        <RouterLink to="/live" aria-label="Live page">
                            <HStack gap={1.5}>
                                <LiveBadge />
                                <Text as="span" aria-hidden="true">&rarr;</Text>
                            </HStack>
                        </RouterLink>
                    </ChakraLink>
                ) : (
                    <Button asChild size="xs" variant="ghost">
                        <RouterLink to="/live">View All</RouterLink>
                    </Button>
                )}
            </HStack>

            {/* What's happening right now leads, then what's still to come, then what's done -
                so the section is worth its place at the top of the page on a matchday. */}
            <VStack align="stretch" gap={4}>
                <MatchGroup title="Live" matches={groups.live} status="During" />
                <MatchGroup title="Coming up" matches={groups.comingUp} status="Pre" />
                <MatchGroup title="Completed" matches={groups.completed} status="Post" />
            </VStack>
        </Panel>
    );
}

type MatchGroupProps = {
    title: string;
    matches: MatchPrediction[];
    status: "Pre" | "During" | "Post";
};

function MatchGroup({ title, matches, status }: MatchGroupProps) {
    if (matches.length === 0) {
        return null;
    }

    const isLive = status === "During";

    return (
        <Box>
            <HStack gap={1.5} mb={1} px={2}>
                {isLive && <LivePulseDot boxSize="7px" />}
                <Text fontSize="xs" fontWeight="bold" letterSpacing="wide" textTransform="uppercase"
                    color={isLive ? "status.live" : "fg.muted"}>
                    {title}
                </Text>
            </HStack>
            <SimpleGrid columns={{ base: 1, xl: matches.length > 3 ? 2 : 1 }} gap={0}>
                {matches.map((match) => (
                    <LiveMatchRow key={match.matchID} match={match} status={status} />
                ))}
            </SimpleGrid>
        </Box>
    );
}
