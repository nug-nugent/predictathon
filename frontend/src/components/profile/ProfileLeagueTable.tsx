import { Heading, Table, Text } from "@chakra-ui/react";
import type { CompetitionUserLeagueTableItem } from "../../services/profile-service";
import { Panel } from "../ui/panel";
import { ShortLabel } from "../ui/short-label";
import { compactCellsOnSmallScreens } from "../ui/table-density";
import { TeamLabel } from "../team/TeamLabel";

// Goals for and against stand down on phones, as they do on the team page's own standings table:
// goal difference is the column that actually separates teams here, and all ten together are wider
// than the screen.
const GOALS_DISPLAY = { base: "none", sm: "table-cell" };

export function ProfileLeagueTable({ username, table }: { username: string; table: CompetitionUserLeagueTableItem[] }) {
    return (
        <Panel overflowX="auto" accent hoverLift>
            <Heading size="md" mb={2}>If {username}'s predictions had all come true...</Heading>
            {table.length === 0 ? (
                <Text color="fg.muted">No league data found</Text>
            ) : (
                <Table.Root size="sm" variant="line" css={compactCellsOnSmallScreens}>
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader textAlign="center">Pos</Table.ColumnHeader>
                            <Table.ColumnHeader>Team</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">P</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">W</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">D</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">L</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center" display={GOALS_DISPLAY}>GF</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center" display={GOALS_DISPLAY}>GA</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center">GD</Table.ColumnHeader>
                            <Table.ColumnHeader textAlign="center"><ShortLabel short="Pts" full="Points" /></Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {table.map((row) => (
                            <Table.Row key={row.teamID}>
                                <Table.Cell textAlign="center">{row.position}</Table.Cell>
                                <Table.Cell><TeamLabel name={null} shortName={row.shortName} acronym={row.acronym} /></Table.Cell>
                                <Table.Cell textAlign="center">{row.played}</Table.Cell>
                                <Table.Cell textAlign="center">{row.won}</Table.Cell>
                                <Table.Cell textAlign="center">{row.drawn}</Table.Cell>
                                <Table.Cell textAlign="center">{row.lost}</Table.Cell>
                                <Table.Cell textAlign="center" display={GOALS_DISPLAY}>{row.goalsFor}</Table.Cell>
                                <Table.Cell textAlign="center" display={GOALS_DISPLAY}>{row.goalsAgainst}</Table.Cell>
                                <Table.Cell textAlign="center">{row.goalDifference}</Table.Cell>
                                <Table.Cell textAlign="center" fontWeight="bold">{row.points}</Table.Cell>
                            </Table.Row>
                        ))}
                    </Table.Body>
                </Table.Root>
            )}
        </Panel>
    );
}
