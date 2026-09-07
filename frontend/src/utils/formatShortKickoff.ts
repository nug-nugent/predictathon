// Formats a match's kick-off as a compact day and time ("9 Sep, 15:30").
//
// Distinct from formatKickoffTime, which is deliberately time-only because everywhere it is used -
// the Predictions list, the Live pages - the day is already established by a heading the match sits
// under. A knockout bracket has no such heading: a tie is placed by its round, not by its date, so
// the only place its day can be said is on the card itself.
//
// No year and no weekday, both of which a bracket column has no room for and neither of which adds
// much: a tournament runs inside a month or two, and the rounds are in order down the page anyway.
export function formatShortKickoff(matchDateTime: string): string {
    return new Date(matchDateTime).toLocaleString(undefined, {
        day: "numeric",
        month: "short",
        hour: "2-digit",
        minute: "2-digit",
    });
}
