import { getJsonAuthenticated } from "./api";
import { getCurrentCompetition } from "./competition-service";

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

/// Fetches the league table for the current competition. Returns an empty table if there's no
/// competition to show one for.
export async function getCurrentLeagueTable(): Promise<LeagueTableItem[]> {
    const competition = await getCurrentCompetition();
    if (!competition) {
        return [];
    }

    return getLeagueTable(competition.competitionID);
}
