import { Box, Heading, HStack, Link as ChakraLink, SimpleGrid, Text, VStack } from "@chakra-ui/react";
import { Radio } from "lucide-react";
import { Link as RouterLink } from "react-router";
import { getTodaysMatches } from "../../services/match-service";
import type { MatchPrediction } from "../../services/prediction-service";
import { groupLiveMatches, hasLiveDayMatches } from "../../utils/liveMatches";
import { useAsyncData } from "../../hooks/useAsyncData";
import { useMinuteTick } from "../../hooks/useMinuteTick";
import { usePolling } from "../../hooks/usePolling";
import { LiveMatchRow } from "../match/live-match-row/LiveMatchRow";
import { LiveBadge } from "../match/live-badge/LiveBadge";
import { Panel } from "../ui/panel";
import { IconChip } from "../ui/icon-chip";
import { ErrorState } from "../ui/async-state";

// A minute is plenty: the only things that change under this card are an admin confirming a result
// and matches crossing their kick-off, and useMinuteTick already re-buckets the latter for free.
const REFRESH_MS = 60000;

/// Today's matches, split into what's still to come, what's in play, and what's finished. Renders
/// nothing at all on days the competition has no matches, which is most of them - the card only
/// earns its place at the top of the Home page on the days it has something to say.
export function LiveUpdatesCard({ competitionId }: { competitionId: string }) {
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
                    <Heading fontSize="17px" fontWeight="bold">Live updates</Heading>
                </HStack>

                {/* The pulsing badge doubles as the way in to the Live page, so the thing drawing
                    the eye is the thing you can click. With nothing in play it falls back to a
                    plain label - the page still has today's card to show. */}
                <ChakraLink asChild variant="underline" fontSize="sm" fontWeight="bold" flexShrink={0}
                    color={isLive ? "status.live" : undefined}>
                    <RouterLink to="/live" aria-label="Live page">
                        <HStack gap={1.5}>
                            {isLive ? <LiveBadge /> : <Text>Live</Text>}
                            <Text as="span" aria-hidden="true">&rarr;</Text>
                        </HStack>
                    </RouterLink>
                </ChakraLink>
            </HStack>

            <VStack align="stretch" gap={4}>
                <MatchGroup title="Coming up" matches={groups.comingUp} status="Pre" />
                <MatchGroup title="Live" matches={groups.live} status="During" />
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

    return (
        <Box>
            <Text fontSize="xs" fontWeight="bold" letterSpacing="wide" textTransform="uppercase"
                color={status === "During" ? "status.live" : "fg.muted"} mb={1} px={2}>
                {title}
            </Text>
            <SimpleGrid columns={{ base: 1, xl: matches.length > 3 ? 2 : 1 }} gap={0}>
                {matches.map((match) => (
                    <LiveMatchRow key={match.matchID} match={match} status={status} />
                ))}
            </SimpleGrid>
        </Box>
    );
}
