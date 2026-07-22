import { getJsonAuthenticated, postJsonAuthenticated } from "./api";
import type { MatchAdmin } from "./match-admin-service";

/// Unplayed matches for a competition whose kickoff has already passed - the pool of matches a
/// result could plausibly be entered for.
export async function getMatchesForProcessing(competitionId: string): Promise<MatchAdmin[]> {
    return getJsonAuthenticated<MatchAdmin[]>(`/Match/Processing/${competitionId}`);
}

/// Records a match's final score and marks it played. Rejected with a 409 if the match hasn't
/// been over long enough yet (see MatchService.SaveResultAsync).
export async function saveMatchResult(matchId: string, homeTeamGoals: number, awayTeamGoals: number): Promise<MatchAdmin> {
    return postJsonAuthenticated<MatchAdmin>("/Match/Result", { matchID: matchId, homeTeamGoals, awayTeamGoals });
}
