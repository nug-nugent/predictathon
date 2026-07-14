import { getJsonAuthenticated } from "./api";

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
