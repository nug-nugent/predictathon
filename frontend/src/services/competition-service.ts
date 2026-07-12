import { getJsonAuthenticated } from "./api";

// Matches Application/Models/UserCompetitionRegistrationListItem.cs.
export type UserCompetitionRegistration = {
    userCompetitionID: string | null;
    competitionID: string;
    competitionName: string;
    imageFilename: string | null;
    startDate: string;
    entranceFee: number;
    registered: boolean;
};

/// Competitions the current user is registered for, most-recently-started first
/// (see CompetitionController.GetMyRegistrations).
export async function getMyRegisteredCompetitions(): Promise<UserCompetitionRegistration[]> {
    return getJsonAuthenticated<UserCompetitionRegistration[]>("/Competition/MyRegistrations");
}
