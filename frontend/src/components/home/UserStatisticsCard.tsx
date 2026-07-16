import { useEffect, useState } from "react";
import { Box, Button, Center, Heading, HStack, Link, Spinner, Table, Text } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import { useUser } from "../../hooks/useUser";
import { getLeagueTable } from "../../services/league-service";
import { getCompetitionWeeks, getMatchesForWeek, getNextUnpredictedMatch, computeDefaultWeek } from "../../services/prediction-service";
import { ordinal } from "../../utils/ordinal";
import { toDateOnly } from "../../utils/toDateOnly";
import { ApiError } from "../../services/api";

type WeekStat = { points: number; position: number };

type Stats = {
    overall: WeekStat | null;
    lastWeek: WeekStat | null;
    thisWeek: WeekStat | null;
    thisWeekLabel: "Current Matches" | "Last Matches";
    nextDueMatchDateTime: string | null;
};

function addDays(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
}

function weekEnd(weekStart: string): string {
    return toDateOnly(addDays(new Date(weekStart), 6));
}

async function findMyRow(competitionId: string, username: string, dateFrom?: string, dateTo?: string): Promise<WeekStat | null> {
    const table = await getLeagueTable(competitionId, dateFrom, dateTo);
    const mine = table.find((r) => r.username === username);
    return mine ? { points: mine.score, position: mine.leaguePosition } : null;
}

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
    const [stats, setStats] = useState<Stats | null>(null);
    const [error, setError] = useState<ApiError | null>(null);

    useEffect(() => {
        if (!user) return;
        let cancelled = false;

        const load = async () => {
            try {
                const [weeks, overall, nextDue] = await Promise.all([
                    getCompetitionWeeks(competitionId),
                    findMyRow(competitionId, user.name),
                    getNextUnpredictedMatch(competitionId),
                ]);

                const currentWeek = computeDefaultWeek(weeks);
                const currentIndex = weeks.indexOf(currentWeek);
                const previousWeek = currentIndex > 0 ? weeks[currentIndex - 1] : null;

                const lastWeek = previousWeek
                    ? await findMyRow(competitionId, user.name, previousWeek, weekEnd(previousWeek))
                    : null;

                // Only show "this week" once at least one match in it has actually been played.
                let thisWeek: WeekStat | null = null;
                if (currentWeek) {
                    const currentWeekMatches = await getMatchesForWeek(competitionId, currentWeek);
                    if (currentWeekMatches.some((m) => m.actualHomeTeamGoals !== null)) {
                        thisWeek = await findMyRow(competitionId, user.name, currentWeek, weekEnd(currentWeek));
                    }
                }

                if (cancelled) return;

                setStats({
                    overall,
                    lastWeek,
                    thisWeek,
                    thisWeekLabel: currentWeek && new Date(weekEnd(currentWeek)) < new Date() ? "Last Matches" : "Current Matches",
                    nextDueMatchDateTime: nextDue?.matchDateTime ?? null,
                });
            } catch (err) {
                if (!cancelled) setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."]));
            }
        };

        load();
        return () => { cancelled = true; };
    }, [competitionId, user]);

    if (!user) return null;

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

    const now = new Date();
    const deadline = stats.nextDueMatchDateTime ? new Date(new Date(stats.nextDueMatchDateTime).getTime() - 5 * 60000) : null;

    return (
        <Box borderWidth="1px" rounded="md" p={4}>
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
        </Box>
    );
}
