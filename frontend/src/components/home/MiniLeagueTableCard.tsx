import { Heading, HStack, Link as ChakraLink, Table } from "@chakra-ui/react";
import { TableProperties } from "lucide-react";
import { Link as RouterLink } from "react-router";
import { getLeagueTable, type LeagueTableItem } from "../../services/league-service";
import { useUser } from "../../hooks/useUser";
import { toDateOnly } from "../../utils/toDateOnly";
import { Panel } from "../ui/panel";
import { IconChip } from "../ui/icon-chip";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../ui/async-state";
import { LeaguePositionChangeIcon } from "../league/LeaguePositionChangeIcon";
import { PlayerAvatar } from "../league/PlayerAvatar";

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
    const ownIndex = user ? table.findIndex((r) => r.userID === user.id) : -1;
    const ownRow = ownIndex >= TOP_ROWS ? table[ownIndex] : undefined;
    // Only show the ellipsis when rows are actually being skipped - the player immediately after the
    // top five follows on directly, so there's no gap to denote.
    const hasGap = ownIndex > TOP_ROWS;

    return (
        <Panel p={3} accent hoverLift>
            <HStack justify="space-between" mb={2}>
                <HStack gap={2}>
                    <IconChip icon={TableProperties} color="action.fg" />
                    <Heading fontSize="17px" fontWeight="bold">League Table</Heading>
                </HStack>
                <ChakraLink asChild fontSize="sm" variant="underline">
                    <RouterLink to="/league">Full table &rarr;</RouterLink>
                </ChakraLink>
            </HStack>
            <Table.Root size="sm" variant="line">
                <Table.Body>
                    {topRows.map((row) => (
                        <MiniLeagueRow key={row.userID} row={row} isCurrentUser={row.userID === user?.id} />
                    ))}
                    {ownRow && (
                        <>
                            {hasGap && (
                                <Table.Row>
                                    <Table.Cell colSpan={4} borderBottomWidth="0" py={0}>&hellip;</Table.Cell>
                                </Table.Row>
                            )}
                            <MiniLeagueRow row={ownRow} isCurrentUser />
                        </>
                    )}
                </Table.Body>
            </Table.Root>
        </Panel>
    );
}

// One row of the mini table. The top-five rows and the current user's own row (appended below when
// they're outside the top five) are identical bar the emphasis, so they share this.
function MiniLeagueRow({ row, isCurrentUser }: { row: LeagueTableItem; isCurrentUser: boolean }) {
    return (
        <Table.Row fontWeight={isCurrentUser ? "bold" : undefined}>
            <Table.Cell textAlign="right" width="1">{row.leaguePosition}</Table.Cell>
            <Table.Cell width="1"><LeaguePositionChangeIcon current={row.leaguePosition} previous={row.previousLeaguePosition} /></Table.Cell>
            <Table.Cell>
                <HStack gap={2}>
                    <PlayerAvatar username={row.username} avatarUrl={row.avatarUrl} />
                    <RouterLink to={`/profile/${row.userID}`}>{row.username}</RouterLink>
                </HStack>
            </Table.Cell>
            <Table.Cell textAlign="right">{row.score}</Table.Cell>
        </Table.Row>
    );
}
