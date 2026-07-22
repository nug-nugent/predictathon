import { getJsonAuthenticated } from "./api";
import { getCompetitionWeeks, computeDefaultWeek } from "./prediction-service";
import { weekEnd } from "../utils/matchWeek";

// Matches Application/Models/LeagueTableItem.cs, as returned by the LeagueTableGet stored procedure.
export type LeagueTableItem = {
    username: string;
    userID: string;
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

export async function getLeagueTable(competitionId: string, dateFrom?: string, dateTo?: string): Promise<LeagueTableItem[]> {
    const params = new URLSearchParams();
    if (dateFrom) params.set("dateFrom", dateFrom);
    if (dateTo) params.set("dateTo", dateTo);
    const query = params.toString();

    return getJsonAuthenticated<LeagueTableItem[]>(`/League/${competitionId}${query ? `?${query}` : ""}`);
}

export type UserWeekStat = { points: number; position: number };

export type UserLeagueStats = {
    overall: UserWeekStat | null;
    lastWeek: UserWeekStat | null;
    thisWeek: UserWeekStat | null;
    // The current match week's start date ("" when the competition has no weeks) - lets callers
    // apply their own current-week rules (e.g. Home only shows thisWeek once a match in it has
    // actually been played).
    currentWeek: string;
};

async function findUserRow(competitionId: string, userId: string, dateFrom?: string, dateTo?: string): Promise<UserWeekStat | null> {
    const table = await getLeagueTable(competitionId, dateFrom, dateTo);
    const mine = table.find((r) => r.userID === userId);
    return mine ? { points: mine.score, position: mine.leaguePosition } : null;
}

/// One user's overall league standing plus their standings within the previous and current match
/// weeks - shared by the Home and Profile statistics cards. Null entries mean the user has no row
/// in that table (e.g. no previous week exists, or they aren't in the competition).
export async function getUserLeagueStats(competitionId: string, userId: string): Promise<UserLeagueStats> {
    const [weeks, overall] = await Promise.all([
        getCompetitionWeeks(competitionId),
        findUserRow(competitionId, userId),
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
