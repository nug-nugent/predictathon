import { getJsonAuthenticated } from "./api";
import type { MatchPrediction } from "./prediction-service";
import type { MatchListItem } from "./statistics-service";

/// Today's matches for a competition, earliest first, each joined with the current user's own
/// prediction - the Home page's Live updates section and the Live page. A match that kicked off
/// shortly before midnight is still included while its result is unconfirmed (LiveDayWindow).
export async function getTodaysMatches(competitionId: string): Promise<MatchPrediction[]> {
    return getJsonAuthenticated<MatchPrediction[]>(`/Match/${competitionId}/Today`);
}

/// Every played match for a competition, most recent first, for the public Results page.
export async function getMatchResults(competitionId: string): Promise<MatchListItem[]> {
    return getJsonAuthenticated<MatchListItem[]>(`/Match/${competitionId}/Results`);
}

/// A single played match's result and prediction stats, for the Match Detail page.
export async function getMatchDetail(competitionId: string, matchId: string): Promise<MatchListItem> {
    return getJsonAuthenticated<MatchListItem>(`/Match/${competitionId}/${matchId}/Detail`);
}
