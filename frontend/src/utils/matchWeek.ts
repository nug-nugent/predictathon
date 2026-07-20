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
