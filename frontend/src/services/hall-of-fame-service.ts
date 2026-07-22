import { getJsonAuthenticated } from "./api";

// Matches Application/Models/HallOfFameListItem.cs.
export type HallOfFameItem = {
    hallOfFameID: string;
    competitionID: string | null;
    competitionName: string | null;
    endDate: string;
    imageFilename: string | null;
    winner: string | null;
    winnerUserID: string | null;
    secondPlace: string | null;
    secondPlaceUserID: string | null;
    thirdPlace: string | null;
    thirdPlaceUserID: string | null;
};

/// Every Hall of Fame entry, most recently concluded competition first.
export async function getHallOfFame(): Promise<HallOfFameItem[]> {
    return getJsonAuthenticated<HallOfFameItem[]>("/HallOfFame");
}
