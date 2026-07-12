import { useState } from "react";
import { Box, Stack, Text } from "@chakra-ui/react";
import { useMinuteTick } from "../../../hooks/useMinuteTick";
import { computeMatchStatus } from "../matchStatus";
import { MatchRow } from "../match-row/MatchRow";
import type { MatchPrediction } from "../../../services/prediction-service";

type MatchListProps = {
    matches: MatchPrediction[];
};

function isPredicted(match: MatchPrediction): boolean {
    return match.homeTeamGoals !== null && match.awayTeamGoals !== null;
}

function findFocusTarget(matches: MatchPrediction[], predictedIds: Set<string>, now: Date): string | null {
    const target = matches.find((m) => computeMatchStatus(m, now).status === "Pre" && !predictedIds.has(m.matchID));
    return target?.matchID ?? null;
}

function formatDateHeading(dateTime: string): string {
    return new Date(dateTime).toLocaleDateString("en-GB", { weekday: "long", day: "numeric", month: "long", year: "numeric" });
}

export function MatchList({ matches }: MatchListProps) {
    const now = useMinuteTick();

    const [predictedIds, setPredictedIds] = useState<Set<string>>(
        () => new Set(matches.filter(isPredicted).map((m) => m.matchID))
    );
    const [focusedMatchId, setFocusedMatchId] = useState<string | null>(
        () => findFocusTarget(matches, predictedIds, now)
    );

    const handleSaved = (matchId: string) => {
        const updated = new Set(predictedIds);
        updated.add(matchId);
        setPredictedIds(updated);

        const index = matches.findIndex((m) => m.matchID === matchId);
        const next = matches.slice(index + 1).find((m) => computeMatchStatus(m, now).status === "Pre" && !updated.has(m.matchID));
        setFocusedMatchId(next?.matchID ?? null);
    };

    if (matches.length === 0) {
        return <Text textAlign="center" py={4}>No matches found.</Text>;
    }

    const showKnockoutWarning = matches.some((m) => m.knockout);
    const dateHeadings = matches.map((m) => formatDateHeading(m.matchDateTime));

    return (
        <Stack gap={0}>
            {matches.map((match, index) => {
                const date = dateHeadings[index];
                const isFirstInGroup = index === 0 || dateHeadings[index - 1] !== date;

                return (
                    <Box key={match.matchID}>
                        {isFirstInGroup && (
                            <Text fontWeight="bold" fontSize="sm" mt={4} mb={1} px={{ base: 2, md: 4 }}>{date}</Text>
                        )}
                        <MatchRow match={match} now={now} hasFocus={match.matchID === focusedMatchId}
                            isFirstInGroup={isFirstInGroup} onFocus={setFocusedMatchId} onSaved={handleSaved} />
                    </Box>
                );
            })}

            {showKnockoutWarning && (
                <Text fontSize="sm" fontStyle="italic" textAlign="center" py={2}>
                    <Text as="span" fontWeight="bold" color="orange.500">*</Text> Extra time excluded
                </Text>
            )}
        </Stack>
    );
}
