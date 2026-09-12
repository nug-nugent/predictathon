import { deleteAuthenticated, getJsonAuthenticated, postJsonAuthenticated, putJsonAuthenticated } from "./api";
import type { MatchListItem } from "./statistics-service";

// Matches Application/Models/TeamModel.cs.
export type Team = {
    teamID: string;
    teamName: string;
    shortName: string;
    acronym: string | null;
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
    /// The group the team is drawn into (e.g. "Group A"), or null where the competition has no
    /// group stage or the team hasn't been placed in one yet.
    groupName: string | null;
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

/// Places a team already assigned to a competition into one of its groups. Pass null to take it
/// out of a group again.
export async function setTeamGroup(teamCompetitionId: string, groupName: string | null): Promise<void> {
    return putJsonAuthenticated<void>(`/Team/${teamCompetitionId}/Group`, { groupName });
}

// Matches Application/Models/TeamDetailModel.cs (TeamFixtureItem).
export type TeamFixture = {
    matchID: string;
    matchDateTime: string;
    homeTeamID: string | null;
    homeTeam: string | null;
    homeTeamShortName: string;
    homeTeamAcronym: string | null;
    homeTeamImage: string | null;
    awayTeamID: string | null;
    awayTeam: string | null;
    awayTeamShortName: string;
    awayTeamAcronym: string | null;
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
    acronym: string | null;
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
    acronym: string | null;
    imageName: string | null;
    /// The group this team is in for the competition (e.g. "Group A"), or null where the
    /// competition has no group stage. Doubles as leagueTable's heading when set.
    groupName: string | null;
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
    /// The table this team sits in: its group's where it has a groupName, the whole competition's
    /// otherwise. Null where neither applies - knockout matches but no groups.
    leagueTable: TeamStanding[] | null;
};

/// A team's played-match stats, results, upcoming fixtures and the table it sits in - its group's
/// where it has a group, the whole competition's otherwise - for the Team Detail page.
// Matches Application/Models/GroupStandingsModel.cs.
export type GroupStandings = {
    groupName: string;
    standings: TeamStanding[];
    /// How many of the group's matches are still to be played.
    matchesRemaining: number;
    /// Whether every match in the group has been played, so the table is final.
    isComplete: boolean;
};

/// Every group's table in a competition, with whether each group has finished.
export async function getGroupStandings(competitionId: string): Promise<GroupStandings[]> {
    return getJsonAuthenticated<GroupStandings[]>(`/Team/${competitionId}/GroupStandings`);
}

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
    homeTeamAcronym: string | null;
    homeTeamImage: string | null;
    awayTeamID: string | null;
    awayTeam: string | null;
    awayTeamShortName: string;
    awayTeamAcronym: string | null;
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
