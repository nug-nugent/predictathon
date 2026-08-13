import { Heading, Table } from "@chakra-ui/react";
import { getUserLeagueStats } from "../../services/league-service";
import { ordinal } from "../../utils/ordinal";
import { Panel } from "../ui/panel";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../ui/async-state";

export function ProfileStatisticsCard({ competitionId, userId }: { competitionId: string; userId: string }) {
    const { data: stats, error } = useAsyncData(() => getUserLeagueStats(competitionId, userId), [competitionId, userId]);

    if (error) {
        return <ErrorState error={error} />;
    }

    if (stats === null) {
        return <LoadingSpinner />;
    }

    return (
        <Panel accent hoverLift>
            <Heading size="md" mb={2}>Statistics</Heading>
            <Table.Root size="sm" variant="line">
                <Table.Body>
                    <Table.Row>
                        <Table.Cell>League position:</Table.Cell>
                        <Table.Cell>{stats.overall ? ordinal(stats.overall.position) : "N/A"}</Table.Cell>
                    </Table.Row>
                    <Table.Row>
                        <Table.Cell>Points:</Table.Cell>
                        <Table.Cell>{stats.overall?.points ?? 0}</Table.Cell>
                    </Table.Row>
                    {stats.lastWeek && (
                        <Table.Row>
                            <Table.Cell>Points last match week:</Table.Cell>
                            <Table.Cell>{stats.lastWeek.points} ({ordinal(stats.lastWeek.position)} place)</Table.Cell>
                        </Table.Row>
                    )}
                    {stats.thisWeek && (
                        <Table.Row>
                            <Table.Cell>Points this match week:</Table.Cell>
                            <Table.Cell>{stats.thisWeek.points} ({ordinal(stats.thisWeek.position)} place)</Table.Cell>
                        </Table.Row>
                    )}
                </Table.Body>
            </Table.Root>
        </Panel>
    );
}
