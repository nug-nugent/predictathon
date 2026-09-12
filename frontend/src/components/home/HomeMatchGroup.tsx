import { Box, HStack, SimpleGrid, Text } from "@chakra-ui/react";
import type { MatchPrediction } from "../../services/prediction-service";
import { computeMatchStatus } from "../match/matchStatus";
import { LiveMatchRow } from "../match/live-match-row/LiveMatchRow";
import { LivePulseDot } from "../match/live-badge/LiveBadge";

type HomeMatchGroupProps = {
    /** What the run of matches has in common - "Coming up", or a day of the week. */
    title: string;
    matches: MatchPrediction[];
    now: Date;
    /** Marks the group as in play: the heading takes the live colour and a pulsing dot. */
    live?: boolean;
    /** Refreshes the card once a quick prediction has been saved, so the row shows the new score. */
    onPredictionSaved: () => void;
};

/// A titled run of matches on one of the Home page's match cards. Each row works out its own
/// Pre/During/Post standing rather than taking the group's, so a group that spans a day - This
/// Week's Matches bands its matches by date, not by stage - still reads each match correctly.
export function HomeMatchGroup({ title, matches, now, live = false, onPredictionSaved }: HomeMatchGroupProps) {
    if (matches.length === 0) {
        return null;
    }

    return (
        <Box>
            <HStack gap={1.5} mb={1} px={2}>
                {live && <LivePulseDot boxSize="7px" />}
                <Text fontSize="xs" fontWeight="bold" letterSpacing="wide" textTransform="uppercase"
                    color={live ? "status.live" : "fg.muted"}>
                    {title}
                </Text>
            </HStack>
            <SimpleGrid columns={{ base: 1, xl: matches.length > 3 ? 2 : 1 }} gap={0}>
                {matches.map((match) => {
                    const { status, minutesToPredict } = computeMatchStatus(match, now);

                    return (
                        <LiveMatchRow key={match.matchID} match={match} status={status}
                            quickPredict minutesToPredict={minutesToPredict}
                            onPredictionSaved={onPredictionSaved} />
                    );
                })}
            </SimpleGrid>
        </Box>
    );
}
