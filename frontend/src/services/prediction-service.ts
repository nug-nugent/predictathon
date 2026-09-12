import { getJsonAuthenticated, postJsonAuthenticated } from "./api";
import { CUTOFF_MINUTES } from "../components/match/matchStatus";

// Matches Application/Models/UserMatchPredictionListItem.cs.
export type MatchPrediction = {
    matchID: string;
    predictionID: string | null;
    matchDateTime: string;
    /** Null for a not-yet-decided knockout placeholder. */
    homeTeamID: string | null;
    homeTeam: string | null;
    homeTeamShortName: string;
    homeTeamAcronym: string | null;
    homeTeamImage: string | null;
    /** Null for a not-yet-decided knockout placeholder. */
    awayTeamID: string | null;
    awayTeam: string | null;
    awayTeamShortName: string;
    awayTeamAcronym: string | null;
    awayTeamImage: string | null;
    homeTeamGoals: number | null;
    awayTeamGoals: number | null;
    actualHomeTeamGoals: number | null;
    actualAwayTeamGoals: number | null;
    /** Whether the result has been confirmed - a match is still in play until this is true. */
    matchPlayed: boolean;
    /**
     * The provisional in-play score, null until something has been heard about the match. Never a
     * confirmed result - that's actualHomeTeamGoals/actualAwayTeamGoals, which stay null while a
     * match is live however this reads.
     */
    liveHomeTeamGoals: number | null;
    liveAwayTeamGoals: number | null;
    /** When the live score last changed - not when it was last confirmed unchanged. */
    liveScoreUpdatedDateTime: string | null;
    /**
     * When the provider was last heard from about this match, whether or not the score moved. Null
     * for a match only ever scored by an admin.
     */
    liveScoreLastPolledDateTime: string | null;
    score: number | null;
    description: string | null;
    knockout: boolean;
    /**
     * Which knockout round this match is in, or null where it isn't part of a bracket. An ordering
     * key valued as the number of teams contesting the round, with 3 reserved for the third-place
     * play-off - see Application/Common/KnockoutRounds.cs. Never do arithmetic on it.
     */
    knockoutRound: number | null;
    /** 1-based position within the round, reading the draw top to bottom. */
    bracketSlot: number | null;
};

/// The Friday-starting weeks a competition has matches in, earliest first.
export async function getCompetitionWeeks(competitionId: string): Promise<string[]> {
    return getJsonAuthenticated<string[]>(`/Competition/${competitionId}/Weeks`);
}

// Matches Application/Models/CompetitionWeekSummary.cs.
export type CompetitionWeekSummary = {
    weekStart: string;
    /** Kick-off of the week's latest match - openness is decided here, against CUTOFF_MINUTES. */
    lastMatchDateTime: string;
    /** Matches in the week the user hasn't predicted and can still predict. */
    openUnpredictedCount: number;
};

/// The competition's weeks summarised for the current user - week boundaries plus what's still
/// outstanding in each. Same weeks getCompetitionWeeks returns, with the extra per-user detail the
/// Predictions page needs.
export async function getCompetitionWeekSummaries(competitionId: string): Promise<CompetitionWeekSummary[]> {
    return getJsonAuthenticated<CompetitionWeekSummary[]>(`/Competition/${competitionId}/WeekSummaries`);
}

export async function getMatchesForWeek(competitionId: string, dateFrom: string): Promise<MatchPrediction[]> {
    return getJsonAuthenticated<MatchPrediction[]>(`/Match/${competitionId}?dateFrom=${encodeURIComponent(dateFrom)}`);
}

// Matches Application/Models/KnockoutBracketModel.cs.
export type KnockoutRound = {
    knockoutRound: number;
    roundName: string;
    /// The round's matches in draw order.
    matches: MatchPrediction[];
};

export type KnockoutBracket = {
    /// The rounds of the tree, first round first. Empty where the competition has no bracket.
    rounds: KnockoutRound[];
    /// Separate from the rounds: a knockout match, but no part of the tree.
    thirdPlacePlayOff: MatchPrediction | null;
    /// False where the bracket is incomplete - the knockout view isn't offered at all rather than
    /// drawing a tree with holes in it. Always the inverse of `problems` being empty.
    isWellFormed: boolean;
    /// What's wrong with the bracket, in words, for the admin screens. Empty where it's sound.
    problems: string[];
};

/// A competition's knockout bracket with the current user's predictions. Comes back with no rounds
/// for a competition that has no bracket, which is how the Predictions page decides whether to offer
/// the knockout view at all.
export async function getKnockoutBracket(competitionId: string): Promise<KnockoutBracket> {
    return getJsonAuthenticated<KnockoutBracket>(`/Match/${competitionId}/Bracket`);
}

export async function savePrediction(matchId: string, homeTeamGoals: number, awayTeamGoals: number, options?: { keepalive?: boolean }): Promise<void> {
    return postJsonAuthenticated<void>("/Prediction", { matchID: matchId, homeTeamGoals, awayTeamGoals }, options);
}

// Matches Application/Models/MatchPredictionListItem.cs.
export type MatchPredictionListItem = {
    predictionID: string;
    username: string;
    userID: string;
    homeTeamGoals: number | null;
    awayTeamGoals: number | null;
    score: number | null;
    /**
     * What the prediction is currently worth against the match's live score. Null when the match has
     * no live score, and when it has a confirmed result - `score` is the real answer there.
     */
    projectedScore: number | null;
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

/// The week-start string a given kick-off falls in, in the same shape the API returns week starts
/// in ("yyyy-MM-ddT00:00:00") - so a ?week= link built from a match lands the Predictions page on
/// that match's week. A mismatch is harmless: PredictionsPage only honours a ?week= it can find in
/// the weeks the API returned, and falls back to its usual landing week otherwise.
export function matchWeekStart(matchDateTime: string): string {
    return formatWeekStart(matchWeekStartDate(new Date(matchDateTime)));
}

/// The week start a given moment falls in, in the same shape - what the Home page's week card
/// tests the week it is showing against, to know whether "this week" is what to call it.
export function currentMatchWeekStart(now: Date): string {
    return formatWeekStart(matchWeekStartDate(now));
}

// A bucketed week start written the way the API writes one ("yyyy-MM-ddT00:00:00"), from the local
// date parts rather than toISOString - which would convert to UTC and, for anywhere east of
// Greenwich in summer, name the day before.
function formatWeekStart(start: Date): string {
    const pad = (value: number) => String(value).padStart(2, "0");

    return `${start.getFullYear()}-${pad(start.getMonth() + 1)}-${pad(start.getDate())}T00:00:00`;
}

/// Picks which week to show by default: the week containing today (if it has matches), the most
/// recent week with matches before today (if today falls in a gap), the competition's first week (if
/// it hasn't started yet), or its last week (if it's already finished). Ported from
/// Predictions.aspx.vb's Page_Load week-selection logic. Returns "" if there are no weeks at all.
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

/// Picks the week /predictions lands on: the earliest week that still contains a predictable match
/// (its last kick-off is more than the save cutoff away), so you arrive somewhere you can still act
/// - entering predictions, or taking a last look at ones already made before the deadline. Testing
/// the week's last match is enough: if that one's still open, at least one match in the week is.
/// Falls back to computeDefaultWeek once nothing is open at all (season over, or every deadline
/// passed), which also covers the empty-competition case.
export function computePredictionsLandingWeek(summaries: CompetitionWeekSummary[], now: Date): string {
    const open = summaries.find(
        (s) => new Date(s.lastMatchDateTime).getTime() - CUTOFF_MINUTES * 60000 > now.getTime()
    );

    return open?.weekStart ?? computeDefaultWeek(summaries.map((s) => s.weekStart));
}

/// Picks the week the Home page's This Week's Matches card covers: the earliest week that still has
/// a match to come. Usually that is simply the week today falls in, which is what the card is for -
/// but a competition week runs Friday to Thursday, so by midweek the current one is often played
/// out, and a card of nothing but results is a card with nothing to do. Rolling on to the next week
/// with fixtures in it keeps the card worth its place (and the heading says which week it landed
/// on). Null once the competition has no matches left at all, which is how the card knows to stand
/// down rather than show a finished season back to itself.
export function computeUpcomingWeek(summaries: CompetitionWeekSummary[], now: Date): string | null {
    return summaries.find((s) => new Date(s.lastMatchDateTime) > now)?.weekStart ?? null;
}
