// Formats a match's day as "9 Sep", with no year and no weekday.
//
// Paired with formatKickoffTime on the knockout bracket's cards, which are the one place a match has
// to carry its own day: a tie there is placed by its round rather than its date, so unlike the
// Predictions list or the Live pages there is no heading above it establishing which day it falls
// on. The year is left off because a tournament runs inside a month or two, and the weekday because
// a bracket column has no room for it.
//
// The month is cut to three letters rather than taken as the locale gives it. CLDR 42 changed
// en-GB's abbreviated September from "Sep" to "Sept", so on a current browser that one month comes
// out a letter wider than the other eleven - which on a bracket card, where this shares a row with
// "Predict", is the difference between fitting and wrapping. Built from formatToParts rather than by
// slicing the finished string so the locale still decides the order: "9 Sep" here, "Sep 9" in en-US.
export function formatShortDate(matchDateTime: string): string {
    const parts = new Intl.DateTimeFormat(undefined, { day: "numeric", month: "short" })
        .formatToParts(new Date(matchDateTime));

    return parts
        .map((part) => (part.type === "month" ? part.value.slice(0, 3) : part.value))
        .join("");
}
