import { useState } from "react";
import { Box, Stack, Text } from "@chakra-ui/react";
import { useMinuteTick } from "../../../../hooks/useMinuteTick";
import { crestUrl } from "../../../../utils/crestUrl";
import { isResultEligible } from "./matchProcessingStatus";
import { ResultRow } from "./ResultRow";
import type { MatchAdmin } from "../../../../services/match-admin-service";
import type { Team } from "../../../../services/team-service";

type ResultsListProps = {
    matches: MatchAdmin[];
    teams: Team[];
};

function teamDisplay(teamId: string | null, tbc: string | null, teams: Team[]): { name: string; crest: string | undefined } {
    const team = teams.find((t) => t.teamID === teamId);
    return {
        name: team?.teamName ?? tbc ?? "TBC",
        crest: crestUrl(team?.imageName ?? null),
    };
}

function formatDateHeading(dateTime: string): string {
    return new Date(dateTime).toLocaleDateString(undefined, { weekday: "long", day: "numeric", month: "long", year: "numeric" });
}

export function ResultsList({ matches, teams }: ResultsListProps) {
    const now = useMinuteTick();

    const [savedIds, setSavedIds] = useState<Set<string>>(new Set());
    const [focusedMatchId, setFocusedMatchId] = useState<string | null>(
        () => matches.find((m) => isResultEligible(m.matchDateTime, now))?.matchID ?? null
    );

    const handleSaved = (matchId: string) => {
        const updated = new Set(savedIds);
        updated.add(matchId);
        setSavedIds(updated);

        const index = matches.findIndex((m) => m.matchID === matchId);
        const next = matches.slice(index + 1).find((m) => isResultEligible(m.matchDateTime, now) && !updated.has(m.matchID));
        setFocusedMatchId(next?.matchID ?? null);
    };

    if (matches.length === 0) {
        return <Text textAlign="center" py={4}>No matches due for results right now.</Text>;
    }

    const dateHeadings = matches.map((m) => formatDateHeading(m.matchDateTime));

    return (
        <Stack gap={0}>
            {matches.map((match, index) => {
                const date = dateHeadings[index];
                const isFirstInGroup = index === 0 || dateHeadings[index - 1] !== date;
                const home = teamDisplay(match.homeTeamID, match.homeTeamTBC, teams);
                const away = teamDisplay(match.awayTeamID, match.awayTeamTBC, teams);

                return (
                    <Box key={match.matchID}>
                        {isFirstInGroup && (
                            <Text fontWeight="bold" fontSize="sm" mt={4} mb={1} px={{ base: 2, md: 4 }}>{date}</Text>
                        )}
                        <ResultRow
                            matchId={match.matchID} matchDateTime={match.matchDateTime}
                            homeTeamName={home.name} awayTeamName={away.name}
                            homeCrest={home.crest} awayCrest={away.crest}
                            now={now} hasFocus={match.matchID === focusedMatchId}
                            isFirstInGroup={isFirstInGroup} onFocus={setFocusedMatchId} onSaved={handleSaved}
                        />
                    </Box>
                );
            })}
        </Stack>
    );
}
