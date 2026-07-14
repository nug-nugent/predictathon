import { deleteAuthenticated, getJsonAuthenticated, postJsonAuthenticated } from "./api";

// Matches Application/Models/TeamModel.cs.
export type Team = {
    teamID: string;
    teamName: string;
    shortName: string;
    imageName: string | null;
};

/// Teams registered for a competition, ordered by name.
export async function getTeamsForCompetition(competitionId: string): Promise<Team[]> {
    return getJsonAuthenticated<Team[]>(`/Team/${competitionId}`);
}

// Matches Application/Models/TeamCompetitionModel.cs.
export type AssignedTeam = {
    teamCompetitionID: string;
    teamID: string;
    teamName: string;
};

/// Teams assigned to a competition including their TeamCompetitionID, ordered by name.
export async function getAssignedTeamsForCompetition(competitionId: string): Promise<AssignedTeam[]> {
    return getJsonAuthenticated<AssignedTeam[]>(`/Team/${competitionId}/Assigned`);
}

/// Teams not yet assigned to a competition, ordered by name (for an "add team" selector).
export async function getUnassignedTeamsForCompetition(competitionId: string): Promise<Team[]> {
    return getJsonAuthenticated<Team[]>(`/Team/${competitionId}/Unassigned`);
}

export async function addTeamToCompetition(competitionId: string, teamId: string): Promise<void> {
    return postJsonAuthenticated<void>(`/Team/${competitionId}/${teamId}`, {});
}

export async function removeTeamFromCompetition(teamCompetitionId: string): Promise<void> {
    return deleteAuthenticated(`/Team/${teamCompetitionId}`);
}
