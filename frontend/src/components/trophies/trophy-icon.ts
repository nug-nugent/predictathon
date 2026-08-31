import { Crown, Medal, Shield, Star, Trophy, type LucideIcon } from "lucide-react";

// A competition series names its badge as a lucide icon name (CompetitionSeries.BadgeIcon), so
// adding a series that reuses one of these needs no code change - only a genuinely new glyph does.
// Lucide can't be indexed dynamically without pulling the whole library into the bundle, so the
// names a series may use are listed explicitly here.
const seriesIcons: Record<string, LucideIcon> = {
    crown: Crown,
    medal: Medal,
    shield: Shield,
    star: Star,
    trophy: Trophy,
};

/// The icon for a series' badge name, falling back to a trophy for a series that names none or
/// names one this build doesn't know about.
export function trophyIcon(badgeIcon: string | null): LucideIcon {
    return (badgeIcon && seriesIcons[badgeIcon]) || Trophy;
}

/// Colour props for a series' badge. A series' own colour is stored as a plain hex string, so it
/// can't be theme-aware the way a token is - several are dark enough (e.g. Premier League purple)
/// to disappear against the dark-mode panel, so dark mode renders a lightened mix of it instead.
/// A series with no colour of its own falls back to the theme's trophy gold, which already is.
export function trophyColour(badgeColour: string | null) {
    if (!badgeColour) {
        return { color: "trophy" };
    }

    return { color: badgeColour, _dark: { color: `color-mix(in oklab, ${badgeColour}, white 45%)` } };
}

/// How a trophy reads out to a screen reader, and what the native tooltip shows on hover.
export function trophyLabel(name: string, winCount: number, years: string): string {
    return winCount > 1 ? `${name}, won ${winCount} times: ${years}` : `${name}, won in ${years}`;
}
