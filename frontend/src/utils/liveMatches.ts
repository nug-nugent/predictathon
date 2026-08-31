import { computeMatchStatus, type MatchStatusValue } from "../components/match/matchStatus";
import { matchWeekStart, type MatchPrediction } from "../services/prediction-service";

// The three buckets today's matches fall into, in the order the Today's Matches section shows them.
export type LiveMatchGroups = {
    /** Kick-off is still far enough away to predict - links to /predictions. */
    comingUp: MatchPrediction[];
    /** Past the prediction cutoff with no confirmed result yet - links to /live. */
    live: MatchPrediction[];
    /** Result confirmed - links to /results. */
    completed: MatchPrediction[];
};

// Buckets today's matches by where they are relative to *now*, reusing the same Pre/During/Post
// rule the Predictions page's rows use (computeMatchStatus), so a match never reads as "coming up"
// here while the Predictions page has already closed it - and vice versa. "During" deliberately
// covers everything without a confirmed result, including a match that finished a while ago but
// whose result nobody has entered yet: from a predictor's point of view it's still in play.
export function groupLiveMatches(matches: MatchPrediction[], now: Date): LiveMatchGroups {
    const groups: LiveMatchGroups = { comingUp: [], live: [], completed: [] };

    for (const match of matches) {
        const { status } = computeMatchStatus(match, now);

        if (status === "Pre") {
            groups.comingUp.push(match);
        } else if (status === "During") {
            groups.live.push(match);
        } else {
            groups.completed.push(match);
        }
    }

    return groups;
}

/// Whether there's anything at all to show - the Today's Matches section hides itself entirely on days
/// with no matches rather than rendering an empty card.
export function hasLiveDayMatches(groups: LiveMatchGroups): boolean {
    return groups.comingUp.length > 0 || groups.live.length > 0 || groups.completed.length > 0;
}

/// Where a finished match's row leads. The Home card sends you to the Results list, since it's a
/// summary of the day and the list is the natural next thing to scan; the Live page sends you to
/// the one match, since you were already looking at matches one at a time.
export type CompletedMatchTarget = "results" | "match";

/// Where a match row takes you depends on what you can still do about it: predict it, watch it, or
/// read how it went. Shared so every list of matches agrees on the first two, and is explicit about
/// the third rather than each caller inventing its own.
export function liveMatchHref(
    match: MatchPrediction,
    status: MatchStatusValue,
    completedTarget: CompletedMatchTarget = "results",
): string {
    if (status === "Pre") {
        return `/predictions?week=${encodeURIComponent(matchWeekStart(match.matchDateTime))}`;
    }

    if (status === "During") {
        return `/live/${match.matchID}`;
    }

    return completedTarget === "match" ? `/match/${match.matchID}` : "/results";
}
