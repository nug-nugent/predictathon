// Renders "in 2 days" / "in 5 hours" / "in 12 minutes" for a countdown to a given deadline. The
// deadline itself is the caller's responsibility - see matchStatus.ts's CUTOFF_MINUTES for the
// prediction-save deadline specifically. Shared by the Home page's PredictionDeadlineCard.
export function formatCountdown(deadline: Date, now: Date): string {
    const totalMinutes = Math.max(0, Math.round((deadline.getTime() - now.getTime()) / 60000));
    const days = Math.floor(totalMinutes / 1440);
    if (days >= 1) return `${days} day${days === 1 ? "" : "s"}`;
    const hours = Math.floor(totalMinutes / 60);
    if (hours >= 1) return `${hours} hour${hours === 1 ? "" : "s"}`;
    return `${totalMinutes} minute${totalMinutes === 1 ? "" : "s"}`;
}

export function countdownColor(deadline: Date, now: Date): string {
    const days = Math.floor((deadline.getTime() - now.getTime()) / 86400000);
    if (days <= 0) return "red.500";
    if (days < 4) return "orange.600";
    return "fg";
}
