import { getJsonAuthenticated, putJsonAuthenticated } from "./api";

// Matches Application/Models/UserCompetitionRegistrationListItem.cs.
export type UserCompetitionRegistration = {
    userCompetitionID: string | null;
    competitionID: string;
    competitionName: string;
    imageFilename: string | null;
    startDate: string;
    entranceFee: number;
    registered: boolean;
    isDefaultCompetition: boolean;
};

/// Competitions the current user is registered for, most-recently-started first
/// (see CompetitionController.GetMyRegistrations).
export async function getMyRegisteredCompetitions(): Promise<UserCompetitionRegistration[]> {
    return getJsonAuthenticated<UserCompetitionRegistration[]>("/Competition/MyRegistrations");
}

/// Every competition either registered to or open for registration, most-recently-started first.
export async function getAllCompetitionRegistrations(): Promise<UserCompetitionRegistration[]> {
    return getJsonAuthenticated<UserCompetitionRegistration[]>("/Competition/Registrations");
}

/// Marks a competition as the current user's default, for it to be preselected on future logins.
export async function setDefaultCompetition(competitionId: string): Promise<void> {
    return putJsonAuthenticated<void>(`/Competition/${competitionId}/SetDefault`, {});
}

// Just the fields consumers outside admin need - the full CompetitionModel shape lives in
// competition-admin-service.ts.
export type CompetitionDetails = {
    competitionID: string;
    competitionName: string;
    duplicateFixturesAllowed: boolean;
};

export async function getCompetitionDetails(competitionId: string): Promise<CompetitionDetails> {
    return getJsonAuthenticated<CompetitionDetails>(`/Competition/${competitionId}`);
}
