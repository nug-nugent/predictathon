import { Avatar, Button, Heading, HStack, Link, Table } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import { useUser } from "../../hooks/useUser";
import { getUserLeagueStats, type UserWeekStat } from "../../services/league-service";
import { getMatchesForWeek } from "../../services/prediction-service";
import { ordinal } from "../../utils/ordinal";
import { weekEnd } from "../../utils/matchWeek";
import { Panel } from "../ui/panel";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../ui/async-state";
import { LeaguePositionChangeIcon } from "../league/LeaguePositionChangeIcon";

type Stats = {
    overall: UserWeekStat | null;
    lastWeek: UserWeekStat | null;
    thisWeek: UserWeekStat | null;
    thisWeekLabel: "Current Matches" | "Last Matches";
};

export function UserStatisticsCard({ competitionId }: { competitionId: string }) {
    const { user } = useUser();
    const userId = user?.id;

    const { data: stats, error } = useAsyncData<Stats | null>(async () => {
        if (!userId) return null;

        const leagueStats = await getUserLeagueStats(competitionId, userId);

        // Only show "this week" once at least one match in it has actually been played.
        let thisWeek = leagueStats.thisWeek;
        if (thisWeek && leagueStats.currentWeek) {
            const currentWeekMatches = await getMatchesForWeek(competitionId, leagueStats.currentWeek);
            if (!currentWeekMatches.some((m) => m.actualHomeTeamGoals !== null)) {
                thisWeek = null;
            }
        }

        const thisWeekLabel: Stats["thisWeekLabel"] =
            leagueStats.currentWeek && new Date(weekEnd(leagueStats.currentWeek)) < new Date()
                ? "Last Matches"
                : "Current Matches";

        return {
            overall: leagueStats.overall,
            lastWeek: leagueStats.lastWeek,
            thisWeek,
            thisWeekLabel,
        };
    }, [competitionId, userId]);

    if (!user) return null;

    if (error) {
        return <ErrorState error={error} />;
    }

    if (stats === null) {
        return <LoadingSpinner />;
    }

    return (
        <Panel p={3}>
            <HStack justify="space-between" mb={2}>
                <HStack gap={3}>
                    <Avatar.Root size="md">
                        <Avatar.Image src={user.avatarUrl} />
                        <Avatar.Fallback name={user.name} />
                    </Avatar.Root>
                    <Heading fontSize="17px" fontWeight="semibold">{user.name}</Heading>
                </HStack>
                <Button asChild size="xs" variant="ghost">
                    <RouterLink to="/profile/edit">Edit User</RouterLink>
                </Button>
            </HStack>
            <Table.Root size="sm" variant="line">
                <Table.Body>
                    <Table.Row>
                        <Table.Cell>League position:</Table.Cell>
                        <Table.Cell>
                            {stats.overall ? (
                                <HStack gap={1}>
                                    <span>{ordinal(stats.overall.position)}</span>
                                    <LeaguePositionChangeIcon current={stats.overall.position} previous={stats.overall.previousPosition} />
                                </HStack>
                            ) : "N/A"}
                        </Table.Cell>
                    </Table.Row>
                    <Table.Row>
                        <Table.Cell>Points:</Table.Cell>
                        <Table.Cell>{stats.overall?.points ?? 0}</Table.Cell>
                    </Table.Row>
                    {stats.lastWeek && (
                        <Table.Row>
                            <Table.Cell>
                                <Link asChild><RouterLink to="/league?date=LastWeek">Points last match week:</RouterLink></Link>
                            </Table.Cell>
                            <Table.Cell>{stats.lastWeek.points} ({ordinal(stats.lastWeek.position)} place)</Table.Cell>
                        </Table.Row>
                    )}
                    {stats.thisWeek && (
                        <Table.Row>
                            <Table.Cell>
                                <Link asChild><RouterLink to="/league?date=ThisWeek">Points {stats.thisWeekLabel.toLowerCase()}:</RouterLink></Link>
                            </Table.Cell>
                            <Table.Cell>{stats.thisWeek.points} ({ordinal(stats.thisWeek.position)} place)</Table.Cell>
                        </Table.Row>
                    )}
                </Table.Body>
            </Table.Root>
        </Panel>
    );
}
