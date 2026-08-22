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

function teamDisplay(teamId: string | null, tbc: string | null, teams: Team[]): { teamId: string | null; name: string; shortName: string; crest: string | undefined } {
    const team = teams.find((t) => t.teamID === teamId);
    return {
        teamId,
        name: team?.teamName ?? tbc ?? "TBC",
        shortName: team?.shortName ?? tbc ?? "TBC",
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
        // Already saved once means this is a correction, not a first-time entry - don't yank
        // focus away to the next match while the admin is still fixing this one.
        if (savedIds.has(matchId)) return;

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

    const groups: { date: string; matches: MatchAdmin[] }[] = [];
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
                        {group.matches.map((match, index) => {
                            const home = teamDisplay(match.homeTeamID, match.homeTeamTBC, teams);
                            const away = teamDisplay(match.awayTeamID, match.awayTeamTBC, teams);

                            return (
                                <ResultRow
                                    key={match.matchID}
                                    matchId={match.matchID} matchDateTime={match.matchDateTime}
                                    homeTeamId={home.teamId} homeTeamName={home.name} homeTeamShortName={home.shortName}
                                    awayTeamId={away.teamId} awayTeamName={away.name} awayTeamShortName={away.shortName}
                                    homeCrest={home.crest} awayCrest={away.crest}
                                    now={now} hasFocus={match.matchID === focusedMatchId}
                                    isFirstInGroup={index === 0} onFocus={setFocusedMatchId} onSaved={handleSaved}
                                />
                            );
                        })}
                    </Box>
                </Box>
            ))}
        </Stack>
    );
}
