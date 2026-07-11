import { getJsonAuthenticated } from "./api";

export type Competition = {
    competitionID: string;
    competitionName: string;
    startDate: string;
    endDate: string;
};

/// Competitions the API returns most-recently-started first (see ICompetitionService.GetCompetitionListAsync).
export async function getCompetitions(): Promise<Competition[]> {
    return getJsonAuthenticated<Competition[]>("/Competition");
}

/// The competition the app should default to when none has been explicitly selected. There's no
/// real "current competition" concept yet - this is an interim stand-in until one exists.
export async function getCurrentCompetition(): Promise<Competition | undefined> {
    const competitions = await getCompetitions();
    return competitions[0];
}
