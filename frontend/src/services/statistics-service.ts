import { getJsonAuthenticated } from "./api";
import type { LeagueTableItem } from "./league-service";

// Matches Application/Models/AllTimeStatisticsModel.cs.
export type CompetitionWinner = { userID: string; username: string; wins: number; secondPlaces: number; thirdPlaces: number };
export type HighestAllTimeScore = { userID: string; username: string; totalScore: number };
export type HighestAverageScore = { userID: string; username: string; averageScore: number };
export type HighestPercentageCorrect = { userID: string; username: string; correctPredictionPercentage: number };
export type MostPredictions = { userID: string; username: string; totalPredictions: number };

export type AllTimeStatistics = {
    competitionWinners: CompetitionWinner[];
    highestAllTimeScores: HighestAllTimeScore[];
    highestAverageScores: HighestAverageScore[];
    highestPercentageCorrect: HighestPercentageCorrect[];
    mostPredictions: MostPredictions[];
};

// Matches Application/Models/CurrentCompetitionStatisticsModel.cs.
export type PredictableTeam = {
    teamID: string;
    shortName: string;
    teamName: string;
    teamImage: string | null;
    averageScore: number;
};

export type MatchListItem = {
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
    homeTeamGoals: number | null;
    awayTeamGoals: number | null;
    predictionHomeTeamGoals: number | null;
    predictionAwayTeamGoals: number | null;
    yourPredictionScore: number;
    averagePredictionScore: number;
    description: string | null;
    knockout: boolean;
};

export type BestPrediction = {
    userID: string;
    username: string;
    matchID: string;
    matchDateTime: string;
    homeTeam: string | null;
    homeTeamShortName: string;
    awayTeam: string | null;
    awayTeamShortName: string;
    homeTeamGoals: number | null;
    awayTeamGoals: number | null;
    predictionHomeTeamGoals: number | null;
    predictionAwayTeamGoals: number | null;
    predictionScore: number;
    averagePredictionScore: number;
    scoreDifference: number;
    description: string | null;
    knockout: boolean;
};

export type CurrentCompetitionStatistics = {
    predictableTeams: PredictableTeam[];
    mostPredictableMatches: MatchListItem[];
    leastPredictableMatches: MatchListItem[];
    bestPredictions: BestPrediction[];
};

/// Every all-time (not competition-scoped) statistics widget.
export async function getAllTimeStatistics(): Promise<AllTimeStatistics> {
    return getJsonAuthenticated<AllTimeStatistics>("/Statistics/AllTime");
}

/// The all-time league table - one row per user across every competition they've ever been
/// registered for, ranked the same way as a single competition's league table.
export async function getAllTimeLeagueTable(): Promise<LeagueTableItem[]> {
    return getJsonAuthenticated<LeagueTableItem[]>("/Statistics/AllTimeLeagueTable");
}

/// Every statistics widget scoped to a specific competition, personalised to the current user.
export async function getCurrentCompetitionStatistics(competitionId: string): Promise<CurrentCompetitionStatistics> {
    return getJsonAuthenticated<CurrentCompetitionStatistics>(`/Statistics/CurrentCompetition/${competitionId}`);
}

/// Predictions that beat the average prediction score for their match, best first, optionally
/// restricted to matches within a date range.
export async function getBestPredictions(competitionId: string, dateFrom?: string, dateTo?: string): Promise<BestPrediction[]> {
    const params = new URLSearchParams();
    if (dateFrom) params.set("dateFrom", dateFrom);
    if (dateTo) params.set("dateTo", dateTo);
    const query = params.toString();

    return getJsonAuthenticated<BestPrediction[]>(`/Statistics/CurrentCompetition/${competitionId}/BestPredictions${query ? `?${query}` : ""}`);
}
