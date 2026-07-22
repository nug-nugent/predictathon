import { getJsonAuthenticated, postJsonAuthenticated, deleteAuthenticated } from "./api";

// Matches Application/Models/PaymentCreditListItem.cs.
export type PaymentCreditAdmin = {
    paymentCreditID: string;
    expectedUsername: string | null;
    competitionID: string;
    competitionName: string;
    uniquePaymentCode: string;
    creditUsed: boolean;
    issueDate: string;
};

// Matches Application/Models/CreatePaymentCreditModel.cs.
export type CreatePaymentCreditAdmin = {
    competitionID: string;
    expectedUsername: string;
};

/// Every payment credit across all competitions, most recently issued first.
export async function getPaymentCredits(): Promise<PaymentCreditAdmin[]> {
    return getJsonAuthenticated<PaymentCreditAdmin[]>("/PaymentCredit");
}

export async function createPaymentCredit(model: CreatePaymentCreditAdmin): Promise<PaymentCreditAdmin> {
    return postJsonAuthenticated<PaymentCreditAdmin>("/PaymentCredit", model);
}

export async function deletePaymentCredit(paymentCreditId: string): Promise<void> {
    return deleteAuthenticated(`/PaymentCredit/${paymentCreditId}`);
}
