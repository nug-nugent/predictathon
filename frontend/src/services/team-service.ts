import { deleteAuthenticated, getJsonAuthenticated, postJsonAuthenticated } from "./api";
import type { MatchListItem } from "./statistics-service";

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

// Matches Application/Models/TeamDetailModel.cs (TeamFixtureItem).
export type TeamFixture = {
    matchID: string;
    matchDateTime: string;
    homeTeamID: string | null;
    homeTeam: string | null;
    homeTeamShortName: string;
    homeTeamImage: string | null;
    awayTeamID: string | null;
    awayTeam: string | null;
    awayTeamShortName: string;
    awayTeamImage: string | null;
    neutralGround: boolean;
    description: string | null;
    knockout: boolean;
};

// Matches Application/Models/TeamDetailModel.cs (TeamStandingItem) - a row of the competition's
// actual football league table, not the users' prediction league (see league-service.ts).
export type TeamStanding = {
    position: number;
    teamID: string;
    teamName: string;
    shortName: string;
    imageName: string | null;
    played: number;
    won: number;
    drawn: number;
    lost: number;
    goalsFor: number;
    goalsAgainst: number;
    goalDifference: number;
    points: number;
};

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
    results: MatchListItem[];
    fixtures: TeamFixture[];
    /// Null for competitions containing knockout matches, where a single table is meaningless.
    leagueTable: TeamStanding[] | null;
};

/// A team's played-match stats, results, upcoming fixtures and (for competitions without knockout
/// matches) the competition's league table, for the Team Detail page.
export async function getTeamDetail(competitionId: string, teamId: string): Promise<TeamDetail> {
    return getJsonAuthenticated<TeamDetail>(`/Team/${competitionId}/${teamId}/Detail`);
}

// Matches Application/Models/TeamRecentResultItem.cs.
export type TeamRecentResult = {
    matchID: string;
    matchDateTime: string;
    homeTeamID: string | null;
    homeTeam: string | null;
    homeTeamShortName: string;
    homeTeamImage: string | null;
    awayTeamID: string | null;
    awayTeam: string | null;
    awayTeamShortName: string;
    awayTeamImage: string | null;
    homeTeamGoals: number;
    awayTeamGoals: number;
    neutralGround: boolean;
    description: string | null;
    knockout: boolean;
    /// From the point of view of the team the results were asked for.
    outcome: "Win" | "Draw" | "Loss";
};

/// A team's most recently played matches in a competition, newest first - the recent-form list
/// shown from a team's name, without the whole Team Detail payload.
export async function getTeamRecentResults(competitionId: string, teamId: string, count = 6): Promise<TeamRecentResult[]> {
    return getJsonAuthenticated<TeamRecentResult[]>(`/Team/${competitionId}/${teamId}/RecentResults?count=${count}`);
}
