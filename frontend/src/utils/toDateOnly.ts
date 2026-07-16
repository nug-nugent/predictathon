// Formats a Date as a "yyyy-MM-dd" DateOnly query string using its LOCAL date components (not
// toISOString, which converts to UTC first and can roll the date back a day in timezones behind
// UTC) - matches match weeks, which are bucketed using local Date methods.
export function toDateOnly(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, "0");
    const d = String(date.getDate()).padStart(2, "0");
    return `${y}-${m}-${d}`;
}
