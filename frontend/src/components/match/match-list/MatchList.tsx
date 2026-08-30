import { useState } from "react";
import { Box, Stack, Text } from "@chakra-ui/react";
import { useMinuteTick } from "../../../hooks/useMinuteTick";
import { computeMatchStatus } from "../matchStatus";
import { MatchRow } from "../match-row/MatchRow";
import type { MatchPrediction } from "../../../services/prediction-service";

type MatchListProps = {
    matches: MatchPrediction[];
    /** Called after each successful save, so the page can keep its outstanding count in step. */
    onPredictionSaved?: (matchId: string) => void;
};

function isPredicted(match: MatchPrediction): boolean {
    return match.homeTeamGoals !== null && match.awayTeamGoals !== null;
}

function findFocusTarget(matches: MatchPrediction[], predictedIds: Set<string>, now: Date): string | null {
    const target = matches.find((m) => computeMatchStatus(m, now).status === "Pre" && !predictedIds.has(m.matchID));
    return target?.matchID ?? null;
}

function formatDateHeading(dateTime: string): string {
    // Browser locale (not a hardcoded one) - matches every other date in the app.
    return new Date(dateTime).toLocaleDateString(undefined, { weekday: "long", day: "numeric", month: "long", year: "numeric" });
}

export function MatchList({ matches, onPredictionSaved }: MatchListProps) {
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
        onPredictionSaved?.(matchId);

        const index = matches.findIndex((m) => m.matchID === matchId);
        const next = matches.slice(index + 1).find((m) => computeMatchStatus(m, now).status === "Pre" && !updated.has(m.matchID));
        setFocusedMatchId(next?.matchID ?? null);
    };

    if (matches.length === 0) {
        return <Text textAlign="center" py={4}>No matches found.</Text>;
    }

    const showKnockoutWarning = matches.some((m) => m.knockout);
    const dateHeadings = matches.map((m) => formatDateHeading(m.matchDateTime));

    const groups: { date: string; matches: MatchPrediction[] }[] = [];
    matches.forEach((match, index) => {
        const date = dateHeadings[index];
        if (index === 0 || dateHeadings[index - 1] !== date) {
            groups.push({ date, matches: [match] });
        } else {
            groups[groups.length - 1].matches.push(match);
        }
    });

    return (
        <Stack gap={4}>
            {groups.map((group) => (
                <Box key={group.date}>
                    <Text fontWeight="bold" fontSize="sm" mb={1} px={{ base: 2, md: 4 }} color="content.dateHeading">{group.date}</Text>
                    <Box bg="surface.card" borderWidth="1px" borderColor="border.card" borderTopWidth="3px"
                        borderTopColor="card.accentStripe" borderRadius="card">
                        {group.matches.map((match, index) => (
                            <MatchRow key={match.matchID} match={match} now={now} hasFocus={match.matchID === focusedMatchId}
                                isFirstInGroup={index === 0} onFocus={setFocusedMatchId} onSaved={handleSaved} />
                        ))}
                    </Box>
                </Box>
            ))}

            {showKnockoutWarning && (
                <Text fontSize="sm" fontStyle="italic" textAlign="center" py={2}>
                    <Text as="span" fontWeight="bold" color="orange.500">**</Text> Extra time excluded
                </Text>
            )}
        </Stack>
    );
}
