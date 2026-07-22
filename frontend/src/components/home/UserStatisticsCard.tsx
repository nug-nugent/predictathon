import { Button, Heading, HStack, Link, Table, Text } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import { useUser } from "../../hooks/useUser";
import { getUserLeagueStats, type UserWeekStat } from "../../services/league-service";
import { getMatchesForWeek, getNextUnpredictedMatch } from "../../services/prediction-service";
import { ordinal } from "../../utils/ordinal";
import { weekEnd } from "../../utils/matchWeek";
import { Panel } from "../ui/panel";
import { useAsyncData } from "../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../ui/async-state";

type Stats = {
    overall: UserWeekStat | null;
    lastWeek: UserWeekStat | null;
    thisWeek: UserWeekStat | null;
    thisWeekLabel: "Current Matches" | "Last Matches";
    nextDueMatchDateTime: string | null;
};

// Renders "in 2 days" / "in 5 hours" / "in 12 minutes" for the countdown to a match's prediction
// deadline (5 minutes before kickoff, mirroring the server-side cutoff).
function formatCountdown(deadline: Date, now: Date): string {
    const totalMinutes = Math.max(0, Math.round((deadline.getTime() - now.getTime()) / 60000));
    const days = Math.floor(totalMinutes / 1440);
    if (days >= 1) return `${days} day${days === 1 ? "" : "s"}`;
    const hours = Math.floor(totalMinutes / 60);
    if (hours >= 1) return `${hours} hour${hours === 1 ? "" : "s"}`;
    return `${totalMinutes} minute${totalMinutes === 1 ? "" : "s"}`;
}

function countdownColor(deadline: Date, now: Date): string {
    const days = Math.floor((deadline.getTime() - now.getTime()) / 86400000);
    if (days <= 0) return "red.500";
    if (days < 4) return "orange.600";
    return "fg";
}

export function UserStatisticsCard({ competitionId }: { competitionId: string }) {
    const { user } = useUser();
    const userId = user?.id;

    const { data: stats, error } = useAsyncData<Stats | null>(async () => {
        if (!userId) return null;

        const [leagueStats, nextDue] = await Promise.all([
            getUserLeagueStats(competitionId, userId),
            getNextUnpredictedMatch(competitionId),
        ]);

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
            nextDueMatchDateTime: nextDue?.matchDateTime ?? null,
        };
    }, [competitionId, userId]);

    if (!user) return null;

    if (error) {
        return <ErrorState error={error} />;
    }

    if (stats === null) {
        return <LoadingSpinner />;
    }

    const now = new Date();
    const deadline = stats.nextDueMatchDateTime ? new Date(new Date(stats.nextDueMatchDateTime).getTime() - 5 * 60000) : null;

    return (
        <Panel>
            <HStack justify="space-between" mb={2}>
                <Heading size="md">{user.name}</Heading>
                <Button asChild size="xs" variant="ghost">
                    <RouterLink to="/profile/edit">Edit User</RouterLink>
                </Button>
            </HStack>
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
                    <Table.Row>
                        <Table.Cell>
                            <Link asChild><RouterLink to="/predictions">Next prediction due in:</RouterLink></Link>
                        </Table.Cell>
                        <Table.Cell>
                            {deadline ? (
                                <Text as="span" color={countdownColor(deadline, now)} fontWeight={deadline.getTime() - now.getTime() < 4 * 86400000 ? "bold" : "normal"}>
                                    {formatCountdown(deadline, now)}
                                </Text>
                            ) : (
                                <Text as="span" color="green.500">All matches predicted!</Text>
                            )}
                        </Table.Cell>
                    </Table.Row>
                </Table.Body>
            </Table.Root>
        </Panel>
    );
}
