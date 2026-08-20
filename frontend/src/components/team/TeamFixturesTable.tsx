import { useState } from "react";
import { Heading, Table, Text, VStack } from "@chakra-ui/react";
import type { TeamFixture } from "../../services/team-service";
import { Panel } from "../ui/panel";
import { TablePagination } from "../ui/table-pagination";

const PAGE_SIZE = 10;

/// Venue from the perspective of the team whose page this is - a neutral-ground match (a World Cup
/// group game, say) is neither home nor away.
function venue(fixture: TeamFixture, teamId: string): string {
    if (fixture.neutralGround) {
        return "Neutral";
    }

    return fixture.homeTeamID === teamId ? "Home" : "Away";
}

/// A team's not-yet-played matches, soonest first, paged client-side (the whole list arrives with
/// the team detail).
export function TeamFixturesTable({ fixtures, teamId }: { fixtures: TeamFixture[]; teamId: string }) {
    const [page, setPage] = useState(1);
    const pageFixtures = fixtures.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

    return (
        <Panel overflowX="auto" accent hoverLift>
            <VStack align="stretch" gap={1}>
                <Heading size="sm" mb={2}>Fixtures</Heading>
                <Table.Root size="sm" variant="line">
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader>Date / time</Table.ColumnHeader>
                            <Table.ColumnHeader>Match</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">Venue</Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {pageFixtures.length === 0 ? (
                            <Table.Row>
                                <Table.Cell colSpan={3}>
                                    <Text color="fg.muted">No upcoming fixtures</Text>
                                </Table.Cell>
                            </Table.Row>
                        ) : pageFixtures.map((f) => (
                            <Table.Row key={f.matchID}>
                                <Table.Cell>{new Date(f.matchDateTime).toLocaleString(undefined, { dateStyle: "short", timeStyle: "short" })}</Table.Cell>
                                <Table.Cell>
                                    <Text as="span" fontWeight={f.homeTeamID === teamId ? "bold" : "normal"}>{f.homeTeamShortName}</Text>
                                    {" vs "}
                                    <Text as="span" fontWeight={f.awayTeamID === teamId ? "bold" : "normal"}>{f.awayTeamShortName}</Text>
                                </Table.Cell>
                                <Table.Cell textAlign="center">{venue(f, teamId)}</Table.Cell>
                            </Table.Row>
                        ))}
                    </Table.Body>
                </Table.Root>

                <TablePagination count={fixtures.length} pageSize={PAGE_SIZE} page={page} onPageChange={setPage} />
            </VStack>
        </Panel>
    );
}
