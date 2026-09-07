// Formats a match's day as "9 Sep", with no year and no weekday.
//
// Paired with formatKickoffTime on the knockout bracket's cards, which are the one place a match has
// to carry its own day: a tie there is placed by its round rather than its date, so unlike the
// Predictions list or the Live pages there is no heading above it establishing which day it falls
// on. The year is left off because a tournament runs inside a month or two, and the weekday because
// a bracket column has no room for it.
export function formatShortDate(matchDateTime: string): string {
    return new Date(matchDateTime).toLocaleDateString(undefined, { day: "numeric", month: "short" });
}
