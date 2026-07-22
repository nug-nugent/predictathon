// Accepts a single digit or empty string; rejects anything else (used to gate score-entry inputs
// that must stay a single 0-9 character, e.g. so onChange can ignore a pasted multi-digit value).
export function parseDigit(raw: string): string | null {
    if (raw === "") return "";
    return /^[0-9]$/.test(raw) ? raw : null;
}
