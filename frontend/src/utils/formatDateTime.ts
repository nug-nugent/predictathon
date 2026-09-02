// Formats a message board timestamp - a post in a thread, or a thread's last activity. Seconds are
// noise on a message board and cost real width on a phone, where this shares a line with a username
// and a trophy stamp, so this is deliberately shorter than a bare toLocaleString().
export function formatDateTime(dateTime: string): string {
    return new Date(dateTime).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" });
}
