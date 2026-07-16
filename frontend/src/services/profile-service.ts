import { getJsonAuthenticated } from "./api";
import type { MatchPrediction } from "./prediction-service";

// Matches Application/Models/UserProfileModel.cs.
export type UserProfile = {
    userID: string;
    username: string;
    caption: string | null;
    location: string | null;
    favouriteTeam: string | null;
    profileText: string | null;
};

export async function getUserProfile(userId: string): Promise<UserProfile> {
    return getJsonAuthenticated<UserProfile>(`/User/${userId}/Profile`);
}

/// A user's prediction history for a competition, most recent first. Future matches are only
/// included when viewing your own history.
export async function getUserPredictionHistory(competitionId: string, userId: string): Promise<MatchPrediction[]> {
    return getJsonAuthenticated<MatchPrediction[]>(`/Match/${competitionId}/UserPredictions/${userId}`);
}

// Matches Application/Models/CompetitionUserLeagueTableItem.cs.
export type CompetitionUserLeagueTableItem = {
    teamID: string;
    position: number;
    played: number;
    won: number;
    lost: number;
    drawn: number;
    shortName: string;
    points: number;
    goalsFor: number;
    goalsAgainst: number;
    goalDifference: number;
};

/// The league table for a competition as it would be had all of a user's predictions come true.
export async function getUserLeagueTable(competitionId: string, userId: string): Promise<CompetitionUserLeagueTableItem[]> {
    return getJsonAuthenticated<CompetitionUserLeagueTableItem[]>(`/Competition/${competitionId}/UserLeagueTable/${userId}`);
}

// Matches Application/Models/UserCompetitionLeagueHistoryItem.cs.
export type UserCompetitionLeagueHistoryItem = {
    username: string;
    date: string;
    score: number;
    leaguePosition: number;
};

/// A user's league-position history for a competition, ordered by date ascending.
export async function getUserLeagueHistory(competitionId: string, userId: string): Promise<UserCompetitionLeagueHistoryItem[]> {
    return getJsonAuthenticated<UserCompetitionLeagueHistoryItem[]>(`/Competition/${competitionId}/UserLeagueHistory/${userId}`);
}
