import { createContext, useContext } from "react";

export type ColorMode = "light" | "dark";
export type ColorModeContextValue = {
    colorMode: ColorMode;
    toggleColorMode: () => void;
};

// Also hardcoded in index.html's pre-mount boot script (which can't import this module) - keep
// the two in sync by hand if this ever changes.
export const STORAGE_KEY = "predictathon-color-mode";

/// What the browser paints its own chrome with - the address bar on mobile Chrome and Safari 15+,
/// the title bar of an installed app - so the window round the page matches the page. Kept to the
/// header's own colour in light mode and to black in dark, which is what the header sits on there.
///
/// Also hardcoded in index.html's pre-mount boot script, for the same reason STORAGE_KEY is: keep
/// the two in sync by hand.
export const THEME_COLORS: Record<ColorMode, string> = {
    light: "#1E4FD1",
    dark: "#000000",
};

export const ColorModeContext = createContext<ColorModeContextValue | null>(null);

export function useColorMode() {
    const ctx = useContext(ColorModeContext);
    if (!ctx) throw new Error("useColorMode must be used within a ColorModeProvider");
    return ctx;
}
