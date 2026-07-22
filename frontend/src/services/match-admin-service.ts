import { getJsonAuthenticated, postJsonAuthenticated, putJsonAuthenticated, deleteAuthenticated } from "./api";

// Matches Application/Models/CreateMatchModel.cs.
export type CreateMatchAdmin = {
    competitionID: string;
    matchDateTime: string;
    homeTeamID: string | null;
    awayTeamID: string | null;
    // Placeholder team name (e.g. "Winner of Group A") used when the actual team isn't known yet.
    // Only meaningful when the corresponding TeamID is null.
    homeTeamTBC: string | null;
    awayTeamTBC: string | null;
    description: string | null;
    homeTeamGoals: number | null;
    awayTeamGoals: number | null;
    neutralGround: boolean;
    knockout: boolean;
    matchPlayed: boolean;
};

// Matches Application/Models/MatchModel.cs.
export type MatchAdmin = CreateMatchAdmin & {
    matchID: string;
};

/// Every match for a competition for admin management, optionally excluding already-played matches.
export async function getMatchesForAdmin(competitionId: string, includePlayed: boolean): Promise<MatchAdmin[]> {
    return getJsonAuthenticated<MatchAdmin[]>(`/Match/Admin/${competitionId}?includePlayed=${includePlayed}`);
}

export async function createMatch(model: CreateMatchAdmin): Promise<MatchAdmin> {
    return postJsonAuthenticated<MatchAdmin>("/Match", model);
}

export async function updateMatch(matchId: string, model: MatchAdmin): Promise<MatchAdmin> {
    return putJsonAuthenticated<MatchAdmin>(`/Match/${matchId}`, model);
}

export async function deleteMatch(matchId: string): Promise<void> {
    return deleteAuthenticated(`/Match/${matchId}`);
}
