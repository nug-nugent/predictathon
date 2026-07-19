import { useEffect, useState } from "react";
import { Center, Heading, Spinner, Table, Text } from "@chakra-ui/react";
import { getLeagueTable } from "../../services/league-service";
import { getCompetitionWeeks, computeDefaultWeek } from "../../services/prediction-service";
import { ordinal } from "../../utils/ordinal";
import { toDateOnly } from "../../utils/toDateOnly";
import { ApiError } from "../../services/api";
import { Panel } from "../ui/panel";

type WeekStat = { points: number; position: number };

type Stats = {
    overall: WeekStat | null;
    lastWeek: WeekStat | null;
    thisWeek: WeekStat | null;
};

function addDays(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
}

function weekEnd(weekStart: string): string {
    return toDateOnly(addDays(new Date(weekStart), 6));
}

async function findRow(competitionId: string, userId: string, dateFrom?: string, dateTo?: string): Promise<WeekStat | null> {
    const table = await getLeagueTable(competitionId, dateFrom, dateTo);
    const mine = table.find((r) => r.userID === userId);
    return mine ? { points: mine.score, position: mine.leaguePosition } : null;
}

export function ProfileStatisticsCard({ competitionId, userId }: { competitionId: string; userId: string }) {
    const [stats, setStats] = useState<Stats | null>(null);
    const [error, setError] = useState<ApiError | null>(null);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            try {
                const [weeks, overall] = await Promise.all([
                    getCompetitionWeeks(competitionId),
                    findRow(competitionId, userId),
                ]);

                const currentWeek = computeDefaultWeek(weeks);
                const currentIndex = weeks.indexOf(currentWeek);
                const previousWeek = currentIndex > 0 ? weeks[currentIndex - 1] : null;

                const lastWeek = previousWeek
                    ? await findRow(competitionId, userId, previousWeek, weekEnd(previousWeek))
                    : null;
                const thisWeek = currentWeek
                    ? await findRow(competitionId, userId, currentWeek, weekEnd(currentWeek))
                    : null;

                if (cancelled) return;

                setStats({ overall, lastWeek, thisWeek });
            } catch (err) {
                if (!cancelled) setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."]));
            }
        };

        load();
        return () => { cancelled = true; };
    }, [competitionId, userId]);

    if (error) {
        return (
            <Center py={4}>
                <Text>{error.messages.join(" ")}</Text>
            </Center>
        );
    }

    if (stats === null) {
        return (
            <Center py={4}>
                <Spinner />
            </Center>
        );
    }

    return (
        <Panel>
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
