// Formats a "yyyy-MM-dd" DateOnly string (see Application/Models/CreateCompetitionModel.cs's
// StartDate/EndDate) for display. Parsed with no timezone designator so it's read as local
// midnight rather than UTC midnight - otherwise toLocaleDateString could roll the date back a day
// in timezones behind UTC.
export function formatDateOnly(dateOnly: string): string {
    return new Date(`${dateOnly}T00:00:00`).toLocaleDateString(undefined, { dateStyle: "medium" });
}
