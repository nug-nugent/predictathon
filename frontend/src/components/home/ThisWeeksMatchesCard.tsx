import { Button, Heading, HStack, VStack } from "@chakra-ui/react";
import { CalendarDays } from "lucide-react";
import { Link as RouterLink } from "react-router";
import {
    computeUpcomingWeek,
    currentMatchWeekStart,
    getCompetitionWeekSummaries,
    getMatchesForWeek,
    type MatchPrediction,
} from "../../services/prediction-service";
import { useAsyncData } from "../../hooks/useAsyncData";
import { useMinuteTick } from "../../hooks/useMinuteTick";
import { HomeMatchGroup } from "./HomeMatchGroup";
import { Panel } from "../ui/panel";
import { IconChip } from "../ui/icon-chip";
import { ErrorState } from "../ui/async-state";

/// What the competition has on this week, for the days it isn't playing today - the same rows as
/// the Today's Matches card, banded by day rather than by stage, and every one of them still open
/// for predictions can be predicted from here. Between matchdays that card has nothing to say and
/// hides itself; this one takes its place so the top of the Home page still leads with the football.
///
/// Picks its own week (see computeUpcomingWeek) and renders nothing at all once the competition has
/// no matches left to come.
export function ThisWeeksMatchesCard({ competitionId }: { competitionId: string }) {
    const now = useMinuteTick();
    const { data: summaries, error, reload } = useAsyncData(() => getCompetitionWeekSummaries(competitionId), [competitionId]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    // No spinner while this settles, for the same reason the Today's Matches card doesn't have one:
    // it may well turn out to have nothing to show, and a spinner that resolves to nothing is worse
    // than a moment of nothing.
    if (summaries === null) {
        return null;
    }

    const weekStart = computeUpcomingWeek(summaries, now);
    if (weekStart === null) {
        return null;
    }

    // Keyed so rolling on to the next week - which happens the minute the current one's last match
    // kicks off - loads that week from scratch rather than leaving the old one on screen behind it.
    return <WeekPanel key={weekStart} competitionId={competitionId} weekStart={weekStart} now={now} />;
}

type WeekPanelProps = {
    competitionId: string;
    /** The week to show, as the API writes week starts ("yyyy-MM-ddT00:00:00"). */
    weekStart: string;
    now: Date;
};

function WeekPanel({ competitionId, weekStart, now }: WeekPanelProps) {
    const { data: matches, error, reload } = useAsyncData(() => getMatchesForWeek(competitionId, weekStart), [competitionId, weekStart]);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (matches === null || matches.length === 0) {
        return null;
    }

    // The week the card landed on is usually the one we're in, but not always - so it says which.
    const isThisWeek = weekStart === currentMatchWeekStart(now);

    return (
        <Panel p={3} accent mb={3}>
            <HStack gap={2} mb={3} justify="space-between">
                <HStack gap={2}>
                    <IconChip icon={CalendarDays} color="brand.accent" />
                    <Heading fontSize="17px" fontWeight="bold">
                        {isThisWeek ? "This Week's Matches" : "Upcoming Matches"}
                    </Heading>
                </HStack>

                {/* Straight to the week that's on screen rather than to whichever one /predictions
                    would otherwise land on, so the page you arrive at is the one you clicked from. */}
                <Button asChild size="xs" variant="ghost">
                    <RouterLink to={`/predictions?week=${encodeURIComponent(weekStart)}`}>View All</RouterLink>
                </Button>
            </HStack>

            <VStack align="stretch" gap={4}>
                {groupByDay(matches).map((day) => (
                    <HomeMatchGroup key={day.date} title={day.date} matches={day.matches} now={now}
                        onPredictionSaved={reload} />
                ))}
            </VStack>
        </Panel>
    );
}

type DayGroup = { date: string; matches: MatchPrediction[] };

// No year, unlike the Predictions page's date headings: a card covering seven days can't be
// ambiguous about which year it means, and the group headings here are the same small uppercase
// caption as "Coming up" on the Today's Matches card rather than a line of their own.
function formatDayHeading(dateTime: string): string {
    // Browser locale (not a hardcoded one) - matches every other date in the app.
    return new Date(dateTime).toLocaleDateString(undefined, { weekday: "long", day: "numeric", month: "long" });
}

// Matches arrive in kick-off order, so a day's run of them is found simply by watching for the
// heading text to change - the same walk MatchList does.
function groupByDay(matches: MatchPrediction[]): DayGroup[] {
    const days: DayGroup[] = [];

    matches.forEach((match) => {
        const date = formatDayHeading(match.matchDateTime);
        const day = days.at(-1);

        if (day?.date === date) {
            day.matches.push(match);
        } else {
            days.push({ date, matches: [match] });
        }
    });

    return days;
}
