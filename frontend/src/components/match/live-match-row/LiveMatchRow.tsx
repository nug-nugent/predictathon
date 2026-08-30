import type { ReactNode } from "react";
import { Box, HStack, Link as ChakraLink, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import type { MatchPrediction } from "../../../services/prediction-service";
import type { MatchStatusValue } from "../matchStatus";
import { liveMatchHref } from "../../../utils/liveMatches";
import { LiveMatchLine } from "../live-match-line/LiveMatchLine";

type LiveMatchRowProps = {
    match: MatchPrediction;
    status: MatchStatusValue;
};

/// One match as a whole-row link: the two teams, and what the match is worth to you on the right.
export function LiveMatchRow({ match, status }: LiveMatchRowProps) {
    return (
        <ChakraLink asChild variant="plain" display="block" borderRadius="8px"
            _hover={{ bg: "bg.muted", textDecoration: "none" }}
            _focusVisible={{ bg: "bg.muted", outline: "2px solid", outlineColor: "input.borderFocus" }}>
            <RouterLink to={liveMatchHref(match, status)}>
                <HStack gap={{ base: 1, md: 2 }} px={2} py={2} width="full">
                    <LiveMatchLine match={match} status={status} />
                    <Box minW={{ base: "54px", md: "78px" }} textAlign="right" flexShrink={0}>
                        <YourPrediction match={match} status={status} />
                    </Box>
                </HStack>
            </RouterLink>
        </ChakraLink>
    );
}

// The one number that makes a match yours: what you predicted, and (once it's settled) what that
// prediction was worth.
function YourPrediction({ match, status }: LiveMatchRowProps): ReactNode {
    const predicted = match.homeTeamGoals !== null && match.awayTeamGoals !== null;

    if (!predicted) {
        return status === "Pre"
            ? <Text fontSize="xs" fontWeight="bold" color="status.urgent">Predict</Text>
            : <Text fontSize="xs" color="fg.muted">No prediction</Text>;
    }

    return (
        <VStack gap={0} align="flex-end">
            <Text fontSize="xs" color="fg.muted">You: {match.homeTeamGoals} - {match.awayTeamGoals}</Text>
            {status === "Post" && (
                <Text fontSize="xs" fontWeight="bold" color={`points.${match.score ?? 0}`}>
                    {match.score ?? 0} {match.score === 1 ? "point" : "points"}
                </Text>
            )}
        </VStack>
    );
}
