const RESULT_ELIGIBLE_MINUTES_AFTER_KICKOFF = 90;

// Kept in lockstep with the server's own eligibility check (MatchService.SaveResultAsync) -
// showing an editable row any earlier would mean a save silently fails right up until the window
// opens.
export function isResultEligible(matchDateTime: string, now: Date): boolean {
    const kickoff = new Date(matchDateTime);
    return now.getTime() >= kickoff.getTime() + RESULT_ELIGIBLE_MINUTES_AFTER_KICKOFF * 60000;
}
