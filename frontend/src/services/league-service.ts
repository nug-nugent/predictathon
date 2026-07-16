import { getJsonAuthenticated } from "./api";

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
