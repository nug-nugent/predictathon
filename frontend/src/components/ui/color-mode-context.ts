import { createContext, useContext } from "react";

export type ColorMode = "light" | "dark";
export type ColorModeContextValue = {
    colorMode: ColorMode;
    toggleColorMode: () => void;
};

// Also hardcoded in index.html's pre-mount boot script (which can't import this module) - keep
// the two in sync by hand if this ever changes.
export const STORAGE_KEY = "predictathon-color-mode";
export const ColorModeContext = createContext<ColorModeContextValue | null>(null);

export function useColorMode() {
    const ctx = useContext(ColorModeContext);
    if (!ctx) throw new Error("useColorMode must be used within a ColorModeProvider");
    return ctx;
}
