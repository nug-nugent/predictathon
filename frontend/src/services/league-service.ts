import { getJsonAuthenticated } from "./api";
import { getCompetitionWeeks, computeDefaultWeek, getMatchesForWeek } from "./prediction-service";
import { weekEnd, weekOver } from "../utils/matchWeek";
import { toDateOnly } from "../utils/toDateOnly";

// Matches Application/Models/LeagueTableItem.cs, as returned by the LeagueTableGet stored procedure.
export type LeagueTableItem = {
    username: string;
    userID: string;
    // Null when the user hasn't uploaded an avatar - callers fall back to their initial.
    avatarUrl: string | null;
    leaguePosition: number;
    previousLeaguePosition: number | null;
    score: number;
    averageGoalDifference: number;
    threePointers: number;
    twoPointers: number;
    onePointers: number;
    noPointers: number;
    noPredictions: number;
};

export async function getLeagueTable(competitionId: string, dateFrom?: string, dateTo?: string, dateForComparison?: string): Promise<LeagueTableItem[]> {
    const params = new URLSearchParams();
    if (dateFrom) params.set("dateFrom", dateFrom);
    if (dateTo) params.set("dateTo", dateTo);
    if (dateForComparison) params.set("dateForComparison", dateForComparison);
    const query = params.toString();

    return getJsonAuthenticated<LeagueTableItem[]>(`/League/${competitionId}${query ? `?${query}` : ""}`);
}

// Matches Application/Models/LiveLeagueTableItem.cs.
export type LiveLeagueTableItem = LeagueTableItem & {
    /** How much of `score` comes from matches still in play, and is therefore provisional. */
    livePoints: number;
};

/// The league table as it would stand if every match in play ended on its current scoreline. Every
/// column already has the live scores applied, and `previousLeaguePosition` is where the user stands
/// on confirmed results alone - so the usual position-change arrow shows the move the live scores
/// have made.
export async function getLiveLeagueTable(competitionId: string): Promise<LiveLeagueTableItem[]> {
    return getJsonAuthenticated<LiveLeagueTableItem[]>(`/League/${competitionId}/Live`);
}

export type UserWeekStat = { points: number; position: number; previousPosition: number | null };

export type UserLeagueStats = {
    overall: UserWeekStat | null;
    lastWeek: UserWeekStat | null;
    thisWeek: UserWeekStat | null;
    // The current match week's start date ("" when the competition has no weeks) - lets callers
    // apply their own current-week rules (e.g. Home only shows thisWeek once a match in it has
    // actually been played).
    currentWeek: string;
};

async function findUserRow(competitionId: string, userId: string, dateFrom?: string, dateTo?: string, dateForComparison?: string): Promise<UserWeekStat | null> {
    const table = await getLeagueTable(competitionId, dateFrom, dateTo, dateForComparison);
    const mine = table.find((r) => r.userID === userId);
    return mine ? { points: mine.score, position: mine.leaguePosition, previousPosition: mine.previousLeaguePosition } : null;
}

export type UserFormWeek = {
    week: string;
    points: number;
    /** The total can still move: at least one match in the week has no confirmed result yet. */
    provisional: boolean;
};

/// A user's points for their most recent match weeks, oldest first - powers the Home page's
/// form-strip widget. Weeks that are over always count, and the week in progress joins them as soon
/// as one of its matches has been played: waiting for the whole calendar week to elapse left the
/// card showing nothing for a match week whose football had already finished. That week stays
/// flagged `provisional` until every match in it has been processed.
export async function getUserRecentForm(competitionId: string, userId: string, count = 5): Promise<UserFormWeek[]> {
    const weeks = await getCompetitionWeeks(competitionId);
    const now = new Date();

    // Weeks come back oldest first, so the ones that are over form a prefix and the first week
    // after them - if the competition has reached it - is the week currently in progress.
    const finishedWeeks = weeks.filter((w) => weekOver(w) <= now);
    const currentWeek: string | undefined = weeks[finishedWeeks.length];

    const currentWeekMatches = currentWeek ? await getMatchesForWeek(competitionId, currentWeek) : [];
    // Same "has anything actually happened yet" rule the Home statistics card applies to its own
    // current-week row.
    const currentWeekStarted = currentWeekMatches.some((m) => m.actualHomeTeamGoals !== null);
    const currentWeekProcessed = currentWeekMatches.every((m) => m.matchPlayed);

    const shownWeeks = currentWeek && currentWeekStarted ? [...finishedWeeks, currentWeek] : finishedWeeks;
    const recentWeeks = shownWeeks.slice(-count);

    const rows = await Promise.all(recentWeeks.map((w) => findUserRow(competitionId, userId, w, weekEnd(w))));

    return recentWeeks.map((week, i) => ({
        week,
        points: rows[i]?.points ?? 0,
        provisional: week === currentWeek && !currentWeekProcessed,
    }));
}

/// One user's overall league standing plus their standings within the previous and current match
/// weeks - shared by the Home and Profile statistics cards. Null entries mean the user has no row
/// in that table (e.g. no previous week exists, or they aren't in the competition).
export async function getUserLeagueStats(competitionId: string, userId: string): Promise<UserLeagueStats> {
    const [weeks, overall] = await Promise.all([
        getCompetitionWeeks(competitionId),
        // Position-change arrows only make sense against the full, unfiltered table - see
        // LeaguePage's dateForComparison usage for the same rule.
        findUserRow(competitionId, userId, undefined, undefined, toDateOnly(new Date())),
    ]);

    const currentWeek = computeDefaultWeek(weeks);
    const currentIndex = weeks.indexOf(currentWeek);
    const previousWeek = currentIndex > 0 ? weeks[currentIndex - 1] : null;

    const [lastWeek, thisWeek] = await Promise.all([
        previousWeek ? findUserRow(competitionId, userId, previousWeek, weekEnd(previousWeek)) : Promise.resolve(null),
        currentWeek ? findUserRow(competitionId, userId, currentWeek, weekEnd(currentWeek)) : Promise.resolve(null),
    ]);

    return { overall, lastWeek, thisWeek, currentWeek };
}
