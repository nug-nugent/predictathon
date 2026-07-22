import { deleteAuthenticated, getJsonAuthenticated, postJsonAuthenticated } from "./api";
import type { PredictableMatch } from "./statistics-service";

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

// Matches Application/Models/TeamDetailModel.cs.
export type TeamDetail = {
    teamID: string;
    teamName: string;
    shortName: string;
    imageName: string | null;
    goalsFor: number;
    goalsAgainst: number;
    averageGoalsForHome: number | null;
    averageGoalsAgainstHome: number | null;
    averageGoalsForAway: number | null;
    averageGoalsAgainstAway: number | null;
    averageGoalsForTotal: number | null;
    averageGoalsAgainstTotal: number | null;
    results: PredictableMatch[];
};

/// A team's played-match stats and results within a competition, for the Team Detail page.
export async function getTeamDetail(competitionId: string, teamId: string): Promise<TeamDetail> {
    return getJsonAuthenticated<TeamDetail>(`/Team/${competitionId}/${teamId}/Detail`);
}
