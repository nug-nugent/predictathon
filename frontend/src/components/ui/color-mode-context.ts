import { createContext, useContext } from "react";

export type ColorMode = "light" | "dark";
export type ColorModeContextValue = {
    colorMode: ColorMode;
    toggleColorMode: () => void;
};

export const STORAGE_KEY = "predictathon-color-mode";
export const ColorModeContext = createContext<ColorModeContextValue | null>(null);

export function useColorMode() {
    const ctx = useContext(ColorModeContext);
    if (!ctx) throw new Error("useColorMode must be used within a ColorModeProvider");
    return ctx;
}
