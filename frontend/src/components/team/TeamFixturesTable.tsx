import { useState } from "react";
import { Flex, HStack, Heading, Stack, Text, VStack } from "@chakra-ui/react";
import type { TeamFixture } from "../../services/team-service";
import { TeamName } from "../match/team-name/TeamName";
import { crestUrl } from "../../utils/crestUrl";
import { Panel } from "../ui/panel";
import { TablePagination } from "../ui/table-pagination";

const PAGE_SIZE = 10;

// Undecided future matches without a real team assigned should never render blank - same rule as
// MatchRow's.
function teamName(preferred: string | null, fallback: string | null): string {
    return preferred || fallback || "TBC";
}

/// A team's not-yet-played matches, soonest first, paged client-side (the whole list arrives with
/// the team detail). Laid out like MatchRow - home team and crest, kick-off, away crest and team -
/// so both sides line up down the list, with the date sat above the kick-off time in the middle.
export function TeamFixturesTable({ fixtures }: { fixtures: TeamFixture[] }) {
    const [page, setPage] = useState(1);
    const pageFixtures = fixtures.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <Panel overflowX="auto" accent hoverLift>
            <VStack align="stretch" gap={1}>
                <Heading size="sm" mb={2}>Fixtures</Heading>

                {pageFixtures.length === 0 ? (
                    <Text color="fg.muted">No upcoming fixtures</Text>
                ) : (
                    <Stack gap={0}>
                        {pageFixtures.map((fixture, index) => {
                            const kickoff = new Date(fixture.matchDateTime);

                            return (
                                <Flex key={fixture.matchID} direction="column" gap={1} py={2}
                                    borderTopWidth={index === 0 ? "0" : "1px"} borderTopColor="border.hairline">
                                    <Flex align="center" gap={{ base: 2, md: 4 }}>
                                        <HStack flex="1" minW="0" justify="flex-end" gap={2}>
                                            <TeamName teamId={fixture.homeTeamID} name={teamName(fixture.homeTeam, fixture.homeTeamShortName)}
                                                shortName={teamName(fixture.homeTeamShortName, fixture.homeTeam)}
                                                crest={crestUrl(fixture.homeTeamImage)} crestPosition="after" />
                                        </HStack>

                                        <VStack gap={0} flexShrink={0} minW="60px">
                                            <Text fontSize="0.9em" fontWeight="bold">
                                                {kickoff.toLocaleDateString(undefined, { day: "numeric", month: "short" })}
                                            </Text>
                                            <Text fontSize="0.7em" color="fg.muted">
                                                {kickoff.toLocaleTimeString(undefined, { timeStyle: "short" })}
                                            </Text>
                                        </VStack>

                                        <HStack flex="1" minW="0" gap={2}>
                                            <TeamName teamId={fixture.awayTeamID} name={teamName(fixture.awayTeam, fixture.awayTeamShortName)}
                                                shortName={teamName(fixture.awayTeamShortName, fixture.awayTeam)}
                                                crest={crestUrl(fixture.awayTeamImage)} crestPosition="before" />
                                        </HStack>
                                    </Flex>

                                    {fixture.description && (
                                        <Text fontSize="0.75em" color="fg.muted" textAlign="center">{fixture.description}</Text>
                                    )}
                                </Flex>
                            );
                        })}
                    </Stack>
                )}

                <TablePagination count={fixtures.length} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
            </VStack>
        </Panel>
    );
}
