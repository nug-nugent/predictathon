import { getJsonAuthenticated, postJsonAuthenticated, putJsonAuthenticated, deleteAuthenticated } from "./api";

// Matches Application/Models/CreateCompetitionModel.cs.
export type CreateCompetitionAdmin = {
    competitionName: string;
    prependNameWithThe: boolean;
    startDate: string;
    endDate: string;
    duplicateFixturesAllowed: boolean;
    openForRegistration: boolean;
    registrationAvailableOnLoginPage: boolean;
    showInHallOfFame: boolean;
    entranceFee: number;
    payPalPaymentAvailable: boolean;
    information: string | null;
    imageFilename: string | null;
    defaultToNeutralGround: boolean;
    allowTwoPointers: boolean;
    externalApiCompetitionCode: string | null;
};

// Matches Application/Models/CompetitionModel.cs.
export type CompetitionAdmin = CreateCompetitionAdmin & {
    competitionID: string;
};

// Matches Application/Models/FixtureImportSummary.cs.
export type FixtureImportSummary = {
    matchesImported: number;
    teamsAdded: number;
    startDate: string;
    endDate: string;
};

/// Every competition, most recently started first.
export async function getAllCompetitions(): Promise<CompetitionAdmin[]> {
    return getJsonAuthenticated<CompetitionAdmin[]>("/Competition");
}

export async function getCompetition(competitionId: string): Promise<CompetitionAdmin> {
    return getJsonAuthenticated<CompetitionAdmin>(`/Competition/${competitionId}`);
}

export async function createCompetition(model: CreateCompetitionAdmin): Promise<CompetitionAdmin> {
    return postJsonAuthenticated<CompetitionAdmin>("/Competition", model);
}

export async function updateCompetition(competitionId: string, model: CompetitionAdmin): Promise<CompetitionAdmin> {
    return putJsonAuthenticated<CompetitionAdmin>(`/Competition/${competitionId}`, model);
}

export async function deleteCompetition(competitionId: string): Promise<void> {
    return deleteAuthenticated(`/Competition/${competitionId}`);
}

/// Imports a competition's full season fixture list (matches and team assignments) from the
/// external data source configured via its ExternalApiCompetitionCode.
export async function importFixtures(competitionId: string): Promise<FixtureImportSummary> {
    return postJsonAuthenticated<FixtureImportSummary>(`/Competition/${competitionId}/ImportFixtures`, undefined);
}
