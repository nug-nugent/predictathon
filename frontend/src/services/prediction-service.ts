import { getJsonAuthenticated, postJsonAuthenticated } from "./api";

// Matches Application/Models/UserMatchPredictionListItem.cs.
export type MatchPrediction = {
    matchID: string;
    predictionID: string | null;
    matchDateTime: string;
    /** Null for a not-yet-decided knockout placeholder. */
    homeTeamID: string | null;
    homeTeam: string | null;
    homeTeamShortName: string;
    homeTeamImage: string | null;
    /** Null for a not-yet-decided knockout placeholder. */
    awayTeamID: string | null;
    awayTeam: string | null;
    awayTeamShortName: string;
    awayTeamImage: string | null;
    homeTeamGoals: number | null;
    awayTeamGoals: number | null;
    actualHomeTeamGoals: number | null;
    actualAwayTeamGoals: number | null;
    score: number | null;
    description: string | null;
    knockout: boolean;
};

/// The Friday-starting weeks a competition has matches in, earliest first.
export async function getCompetitionWeeks(competitionId: string): Promise<string[]> {
    return getJsonAuthenticated<string[]>(`/Competition/${competitionId}/Weeks`);
}

export async function getMatchesForWeek(competitionId: string, dateFrom: string): Promise<MatchPrediction[]> {
    return getJsonAuthenticated<MatchPrediction[]>(`/Match/${competitionId}?dateFrom=${encodeURIComponent(dateFrom)}`);
}

/// The earliest not-yet-predicted match at least 5 minutes from now, or null if everything's predicted.
export async function getNextUnpredictedMatch(competitionId: string): Promise<MatchPrediction | null> {
    return getJsonAuthenticated<MatchPrediction | null>(`/Match/${competitionId}/NextUnpredicted`);
}

export async function savePrediction(matchId: string, homeTeamGoals: number, awayTeamGoals: number): Promise<void> {
    return postJsonAuthenticated<void>("/Prediction", { matchID: matchId, homeTeamGoals, awayTeamGoals });
}

// Matches Application/Models/MatchPredictionListItem.cs.
export type MatchPredictionListItem = {
    predictionID: string;
    username: string;
    userID: string;
    homeTeamGoals: number | null;
    awayTeamGoals: number | null;
    score: number | null;
};

/// Every registered competitor's prediction for a match. Only available once the match is within
/// 2 minutes of kick-off - rejected with a 409 before that.
export async function getMatchPredictions(matchId: string): Promise<MatchPredictionListItem[]> {
    return getJsonAuthenticated<MatchPredictionListItem[]>(`/Prediction/Match/${matchId}`);
}

// Buckets a date to the start (Friday) of its match week - mirrors CompetitionService.GetCompetitionWeeksAsync's
// bucketing exactly, so this stays consistent with the week boundaries the API returns.
function matchWeekStartDate(date: Date): Date {
    const knownFriday = new Date(1990, 0, 5);
    const diffDays = Math.floor((date.getTime() - knownFriday.getTime()) / 86400000);
    const mod = ((diffDays % 7) + 7) % 7;

    const bucketed = new Date(date);
    bucketed.setHours(0, 0, 0, 0);
    bucketed.setDate(bucketed.getDate() - mod);
    return bucketed;
}

/// Picks which week to show by default: the week containing today (if it has matches), the next
/// upcoming week with matches (if today falls in a gap), the competition's first week (if it hasn't
/// started yet), or its last week (if it's already finished). Ported from Predictions.aspx.vb's
/// Page_Load week-selection logic. Returns "" if the competition has no weeks at all.
export function computeDefaultWeek(weeks: string[]): string {
    if (weeks.length === 0) {
        return "";
    }

    const todayWeek = matchWeekStartDate(new Date());
    const nextWeek = weeks.find((w) => new Date(w) > todayWeek);

    if (!nextWeek) {
        return weeks[weeks.length - 1];
    }
    if (nextWeek === weeks[0]) {
        return nextWeek;
    }

    return weeks[weeks.indexOf(nextWeek) - 1];
}
