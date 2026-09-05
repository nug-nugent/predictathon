// Formats a match's kick-off as a bare local time ("15:00"). The day it falls on is always
// established by whatever the caller sits under - the Predictions list's date headings, the Live
// page's "today" framing - so repeating the date per match would only be noise. Shared so the
// Predictions rows and the Live pages all read the same.
export function formatKickoffTime(matchDateTime: string): string {
    return new Date(matchDateTime).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" });
}
