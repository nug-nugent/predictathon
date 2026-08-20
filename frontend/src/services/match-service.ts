import { getJsonAuthenticated } from "./api";
import type { MatchListItem } from "./statistics-service";

/// Every played match for a competition, most recent first, for the public Results page.
export async function getMatchResults(competitionId: string): Promise<MatchListItem[]> {
    return getJsonAuthenticated<MatchListItem[]>(`/Match/${competitionId}/Results`);
}

/// A single played match's result and prediction stats, for the Match Detail page.
export async function getMatchDetail(competitionId: string, matchId: string): Promise<MatchListItem> {
    return getJsonAuthenticated<MatchListItem>(`/Match/${competitionId}/${matchId}/Detail`);
}
