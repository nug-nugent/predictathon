import { getJson, postJsonAuthenticated } from "./api";

// Matches Application/Models/CompetitionRegistrationSummaryModel.cs.
export type CompetitionRegistrationSummary = {
    competitionID: string;
    competitionName: string;
    startDate: string;
    endDate: string;
    entranceFee: number;
    information: string | null;
    imageFilename: string | null;
    allowTwoPointers: boolean;
};

/// Competitions currently open for registration, for the pre-login landing page.
export async function getCompetitionsOpenForRegistration(): Promise<CompetitionRegistrationSummary[]> {
    return getJson<CompetitionRegistrationSummary[]>("/Competition/OpenForRegistration");
}

// Matches Application/Models/PublicStatsModel.cs.
export type PublicStats = {
    predictionsMadeCount: number;
    completedCompetitionsCount: number;
};

/// Site-wide stats shown on the pre-login landing page.
export async function getPublicStats(): Promise<PublicStats> {
    return getJson<PublicStats>("/Competition/PublicStats");
}

// Just the fields the registration flow needs to display - the full CompetitionModel shape lives
// in competition-admin-service.ts.
export type CompetitionRegistrationDetails = {
    competitionID: string;
    competitionName: string;
    startDate: string;
    endDate: string;
    entranceFee: number;
    payPalPaymentAvailable: boolean;
    information: string | null;
    imageFilename: string | null;
};

/// A competition's registration-relevant details. Public - reachable before login from /register.
export async function getCompetitionForRegistration(competitionId: string): Promise<CompetitionRegistrationDetails> {
    return getJson<CompetitionRegistrationDetails>(`/Competition/${competitionId}`);
}

/// Registers the current user for a competition that has no entrance fee.
export async function registerFree(competitionId: string): Promise<void> {
    await postJsonAuthenticated<void>(`/Competition/${competitionId}/Register/Free`, {});
}

/// Registers the current user for a competition by redeeming a payment credit code.
export async function registerWithPaymentCredit(competitionId: string, code: string): Promise<void> {
    await postJsonAuthenticated<void>(`/Competition/${competitionId}/Register/PaymentCredit`, { code });
}

// Matches Application/Models/PayPalOrderModel.cs.
type PayPalOrder = {
    orderId: string;
};

/// Creates a PayPal order for a competition's entrance fee, for PayPal Buttons to render.
export async function createPayPalOrder(competitionId: string): Promise<PayPalOrder> {
    return postJsonAuthenticated<PayPalOrder>(`/Competition/${competitionId}/Register/PayPal/CreateOrder`, {});
}

/// Captures a buyer-approved PayPal order, registering the current user for the competition.
export async function capturePayPalOrder(competitionId: string, orderId: string): Promise<void> {
    await postJsonAuthenticated<void>(`/Competition/${competitionId}/Register/PayPal/Capture`, { orderId });
}
