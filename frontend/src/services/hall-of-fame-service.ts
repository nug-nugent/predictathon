import { getJsonAuthenticated, postJsonAuthenticated } from "./api";

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

// Matches Application/Models/HallOfFameGenerationStatus.cs.
export type HallOfFameGenerationStatus = {
    allMatchesPlayed: boolean;
    alreadyGenerated: boolean;
};

/// Every Hall of Fame entry, most recently concluded competition first.
export async function getHallOfFame(): Promise<HallOfFameItem[]> {
    return getJsonAuthenticated<HallOfFameItem[]>("/HallOfFame");
}

/// Whether a competition is currently eligible to have its Hall of Fame entry auto-generated.
export async function getHallOfFameGenerationStatus(competitionId: string): Promise<HallOfFameGenerationStatus> {
    return getJsonAuthenticated<HallOfFameGenerationStatus>(`/HallOfFame/${competitionId}/GenerationStatus`);
}

/// Generates a competition's Hall of Fame entry (1st/2nd/3rd place) from its live league table.
export async function generateHallOfFame(competitionId: string): Promise<HallOfFameItem> {
    return postJsonAuthenticated<HallOfFameItem>(`/HallOfFame/${competitionId}/Generate`, undefined);
}
