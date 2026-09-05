import { useState } from "react";
import { Box, Stack, Text } from "@chakra-ui/react";
import { useMinuteTick } from "../../../hooks/useMinuteTick";
import { computeMatchStatus } from "../matchStatus";
import { MatchRow } from "../match-row/MatchRow";
import type { MatchPrediction } from "../../../services/prediction-service";
import { formatKickoffTime } from "../../../utils/formatKickoffTime";

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

type KickoffGroup = { time: string; matches: MatchPrediction[] };
type DayGroup = { date: string; kickoffs: KickoffGroup[] };

// Matches arrive in kick-off order, so a day's card splits into runs of matches sharing a kick-off
// simply by watching for the heading text to change - the same walk the date grouping does one
// level up. Two levels rather than one flat "date + time" heading because a matchday commonly has
// three or four kick-off times, and repeating the date above each of them is noise.
function groupByDayAndKickoff(matches: MatchPrediction[]): DayGroup[] {
    const days: DayGroup[] = [];

    matches.forEach((match) => {
        const date = formatDateHeading(match.matchDateTime);
        const time = formatKickoffTime(match.matchDateTime);

        const day = days.at(-1);
        if (day?.date !== date) {
            days.push({ date, kickoffs: [{ time, matches: [match] }] });
            return;
        }

        const kickoff = day.kickoffs.at(-1);
        if (kickoff?.time === time) {
            kickoff.matches.push(match);
        } else {
            day.kickoffs.push({ time, matches: [match] });
        }
    });

    return days;
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

    // Only worth explaining when a row actually carries the marker - see MatchRow.
    const showKnockoutWarning = matches.some((m) => m.knockout);
    const days = groupByDayAndKickoff(matches);

    return (
        <Stack gap={4}>
            {days.map((day) => (
                <Box key={day.date}>
                    <Text fontWeight="bold" fontSize="sm" mb={1} px={{ base: 2, md: 4 }} color="content.dateHeading">{day.date}</Text>
                    {/* overflow="hidden" so the full-bleed kick-off bands are clipped to the card's
                        rounded corners instead of squaring off its top and bottom. */}
                    <Box bg="surface.card" borderWidth="1px" borderColor="border.card" borderTopWidth="3px"
                        borderTopColor="card.accentStripe" borderRadius="card" overflow="hidden">
                        {day.kickoffs.map((kickoff, kickoffIndex) => (
                            <Box key={kickoff.time}>
                                {/* The kick-off time is a band over the matches that share it rather than a
                                    line on each of them - the same time repeated down a card reads as noise,
                                    and a phone can't spare the row. */}
                                <Text fontSize="xs" fontWeight="bold" color="fg.muted" letterSpacing="wide"
                                    bg="bg.muted" px={{ base: 2, md: 4 }} py={1}
                                    borderTopWidth={kickoffIndex === 0 ? "0" : "1px"} borderTopColor="border.hairline">
                                    {kickoff.time}
                                </Text>

                                {kickoff.matches.map((match, index) => (
                                    <MatchRow key={match.matchID} match={match} now={now} hasFocus={match.matchID === focusedMatchId}
                                        isFirstInGroup={index === 0} onFocus={setFocusedMatchId} onSaved={handleSaved} />
                                ))}
                            </Box>
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
