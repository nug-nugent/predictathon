import { getJsonAuthenticated, postJsonAuthenticated } from "./api";

// Matches Application/Models/FixtureChangeProposalModel.cs.
export type FixtureChangeProposal = {
    fixtureChangeProposalID: number;
    matchID: string;
    homeTeamName: string;
    awayTeamName: string;
    previousMatchDateTime: string;
    proposedMatchDateTime: string;
    detectedAtUtc: string;
};

/// Every pending fixture-change proposal, earliest proposed kickoff first.
export async function getPendingFixtureChanges(): Promise<FixtureChangeProposal[]> {
    return getJsonAuthenticated<FixtureChangeProposal[]>("/FixtureChangeProposal");
}

/// Applies a pending proposal's new kickoff time to its match.
export async function confirmFixtureChange(proposalId: number): Promise<void> {
    await postJsonAuthenticated(`/FixtureChangeProposal/${proposalId}/Confirm`, undefined);
}

/// Dismisses a pending proposal without changing its match.
export async function dismissFixtureChange(proposalId: number): Promise<void> {
    await postJsonAuthenticated(`/FixtureChangeProposal/${proposalId}/Dismiss`, undefined);
}

/// Runs a fixture-change detection pass immediately, rather than waiting for the next scheduled interval.
export async function syncFixtureChangesNow(): Promise<void> {
    await postJsonAuthenticated("/FixtureChangeProposal/SyncNow", undefined);
}
