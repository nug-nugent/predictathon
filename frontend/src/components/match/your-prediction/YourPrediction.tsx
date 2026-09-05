import type { ReactNode } from "react";
import { Text, VStack } from "@chakra-ui/react";
import type { MatchPrediction } from "../../../services/prediction-service";
import type { MatchStatusValue } from "../matchStatus";

/// The one number that makes a match yours: what you predicted, and (once it's settled) what that
/// prediction was worth. Shared by every list of matches, so a match reads the same wherever you
/// meet it - the Home card, the Live page's list of what else is on, and the days-with-no-match
/// fallback in between.
export function YourPrediction({ match, status }: { match: MatchPrediction; status: MatchStatusValue }): ReactNode {
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
