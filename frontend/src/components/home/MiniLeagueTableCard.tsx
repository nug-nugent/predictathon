import { Heading, HStack, Link as ChakraLink, Table } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import { getLeagueTable } from "../../services/league-service";
import { useUser } from "../../hooks/useUser";
import { toDateOnly } from "../../utils/toDateOnly";
import { Panel } from "../ui/panel";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../ui/async-state";
import { LeaguePositionChangeIcon } from "../league/LeaguePositionChangeIcon";

const TOP_ROWS = 5;

export function MiniLeagueTableCard({ competitionId }: { competitionId: string }) {
    const { user } = useUser();

    const { data: table, error } = useAsyncData(
        () => getLeagueTable(competitionId, undefined, undefined, toDateOnly(new Date())),
        [competitionId],
    );

    if (error) {
        return <ErrorState error={error} />;
    }

    if (table === null) {
        return <LoadingSpinner />;
    }

    const topRows = table.slice(0, TOP_ROWS);
    const ownRow = user && !topRows.some((r) => r.userID === user.id) ? table.find((r) => r.userID === user.id) : undefined;

    return (
        <Panel p={3}>
            <HStack justify="space-between" mb={2}>
                <Heading fontSize="17px" fontWeight="semibold">League Table</Heading>
                <ChakraLink asChild fontSize="sm" variant="underline">
                    <RouterLink to="/league">Full table &rarr;</RouterLink>
                </ChakraLink>
            </HStack>
            <Table.Root size="sm" variant="line">
                <Table.Body>
                    {topRows.map((row) => (
                        <Table.Row key={row.userID} fontWeight={row.userID === user?.id ? "bold" : undefined}>
                            <Table.Cell textAlign="right" width="1">{row.leaguePosition}</Table.Cell>
                            <Table.Cell width="1"><LeaguePositionChangeIcon current={row.leaguePosition} previous={row.previousLeaguePosition} /></Table.Cell>
                            <Table.Cell><RouterLink to={`/profile/${row.userID}`}>{row.username}</RouterLink></Table.Cell>
                            <Table.Cell textAlign="right">{row.score}</Table.Cell>
                        </Table.Row>
                    ))}
                    {ownRow && (
                        <>
                            <Table.Row>
                                <Table.Cell colSpan={4} borderBottomWidth="0" py={0}>&hellip;</Table.Cell>
                            </Table.Row>
                            <Table.Row fontWeight="bold">
                                <Table.Cell textAlign="right" width="1">{ownRow.leaguePosition}</Table.Cell>
                                <Table.Cell width="1"><LeaguePositionChangeIcon current={ownRow.leaguePosition} previous={ownRow.previousLeaguePosition} /></Table.Cell>
                                <Table.Cell><RouterLink to={`/profile/${ownRow.userID}`}>{ownRow.username}</RouterLink></Table.Cell>
                                <Table.Cell textAlign="right">{ownRow.score}</Table.Cell>
                            </Table.Row>
                        </>
                    )}
                </Table.Body>
            </Table.Root>
        </Panel>
    );
}
