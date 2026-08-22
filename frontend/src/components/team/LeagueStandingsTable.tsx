import { HStack, Heading, Image, Table, Text, VStack } from "@chakra-ui/react";
import { Link } from "react-router";
import type { TeamStanding } from "../../services/team-service";
import { crestUrl } from "../../utils/crestUrl";
import { Panel } from "../ui/panel";

/// The competition's actual football league table (not the users' prediction league - see
/// LeagueTableView), with the team whose page this is picked out.
export function LeagueStandingsTable({ standings, highlightTeamId }: { standings: TeamStanding[]; highlightTeamId: string }) {
    return (
        <Panel overflowX="auto" accent>
            <VStack align="stretch" gap={1}>
                <Heading size="sm" mb={2}>League table</Heading>
                <Table.Root size="sm" variant="line" showColumnBorder stickyHeader>
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader fontWeight="bold" fontSize="0.8em" textAlign="center">POS</Table.ColumnHeader>
                            <Table.ColumnHeader fontWeight="bold" fontSize="0.8em">TEAM</Table.ColumnHeader>
                            <Table.ColumnHeader fontWeight="bold" fontSize="0.8em" textAlign="center">P</Table.ColumnHeader>
                            <Table.ColumnHeader fontWeight="bold" fontSize="0.8em" textAlign="center">W</Table.ColumnHeader>
                            <Table.ColumnHeader fontWeight="bold" fontSize="0.8em" textAlign="center">D</Table.ColumnHeader>
                            <Table.ColumnHeader fontWeight="bold" fontSize="0.8em" textAlign="center">L</Table.ColumnHeader>
                            <Table.ColumnHeader fontWeight="bold" fontSize="0.8em" textAlign="center" display={{ base: "none", sm: "table-cell" }}>GF</Table.ColumnHeader>
                            <Table.ColumnHeader fontWeight="bold" fontSize="0.8em" textAlign="center" display={{ base: "none", sm: "table-cell" }}>GA</Table.ColumnHeader>
                            <Table.ColumnHeader fontWeight="bold" fontSize="0.8em" textAlign="center">GD</Table.ColumnHeader>
                            <Table.ColumnHeader fontWeight="bold" fontSize="0.8em" textAlign="center">PTS</Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {standings.length === 0 ? (
                            <Table.Row>
                                <Table.Cell colSpan={10}>
                                    <Text color="fg.muted">No teams found</Text>
                                </Table.Cell>
                            </Table.Row>
                        ) : standings.map((standing) => {
                            const isHighlighted = standing.teamID === highlightTeamId;
                            const crest = crestUrl(standing.imageName);

                            return (
                                <Table.Row key={standing.teamID} bg={isHighlighted ? "surface.highlightRow" : undefined} fontWeight={isHighlighted ? "bold" : "normal"}>
                                    <Table.Cell fontSize="0.9em" textAlign="center">{standing.position}</Table.Cell>
                                    <Table.Cell fontSize="0.9em">
                                        <HStack gap={2} minW="0">
                                            {crest && <Image src={crest} boxSize="20px" objectFit="contain" alt="" flexShrink={0} />}
                                            <Link to={`/team/${standing.teamID}`}>
                                                <Text as="span" hideFrom="md">{standing.shortName}</Text>
                                                <Text as="span" hideBelow="md">{standing.teamName}</Text>
                                            </Link>
                                        </HStack>
                                    </Table.Cell>
                                    <Table.Cell fontSize="0.9em" textAlign="center">{standing.played}</Table.Cell>
                                    <Table.Cell fontSize="0.9em" textAlign="center">{standing.won}</Table.Cell>
                                    <Table.Cell fontSize="0.9em" textAlign="center">{standing.drawn}</Table.Cell>
                                    <Table.Cell fontSize="0.9em" textAlign="center">{standing.lost}</Table.Cell>
                                    <Table.Cell fontSize="0.9em" textAlign="center" display={{ base: "none", sm: "table-cell" }}>{standing.goalsFor}</Table.Cell>
                                    <Table.Cell fontSize="0.9em" textAlign="center" display={{ base: "none", sm: "table-cell" }}>{standing.goalsAgainst}</Table.Cell>
                                    <Table.Cell fontSize="0.9em" textAlign="center">{standing.goalDifference}</Table.Cell>
                                    <Table.Cell fontSize="0.9em" textAlign="center">{standing.points}</Table.Cell>
                                </Table.Row>
                            );
                        })}
                    </Table.Body>
                </Table.Root>
            </VStack>
        </Panel>
    );
}
