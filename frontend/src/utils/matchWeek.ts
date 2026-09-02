import { toDateOnly } from "./toDateOnly";

export function addDays(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
}

// The last day (Thursday) of a Friday-starting match week, as a "yyyy-MM-dd" DateOnly string -
// pairs with a week-start date to form the dateFrom/dateTo range for league-table queries.
export function weekEnd(weekStart: string): string {
    return toDateOnly(addDays(new Date(weekStart), 6));
}

// The instant a Friday-starting match week is over: midnight local time at the end of its final
// (Thursday) day. Distinct from weekEnd, which is the *inclusive* last date of the week and is only
// safe as a date-only league-table boundary - parsing that string back into a Date gives UTC
// midnight, i.e. the start of the Thursday rather than the end of it, so a week reads as finished
// almost a day early anywhere at or ahead of UTC.
export function weekOver(weekStart: string): Date {
    return addDays(new Date(weekStart), 7);
}
