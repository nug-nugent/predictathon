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

export async function getLeagueTable(competitionId: string): Promise<LeagueTableItem[]> {
    return getJsonAuthenticated<LeagueTableItem[]>(`/League/${competitionId}`);
}
