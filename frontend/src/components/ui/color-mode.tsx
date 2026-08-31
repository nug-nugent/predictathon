import { useEffect, useState, type ReactNode } from "react";
import { ColorModeContext, STORAGE_KEY, THEME_COLORS, type ColorMode } from "./color-mode-context";

function getInitialColorMode(): ColorMode {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    if (stored === "light" || stored === "dark") return stored;
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function ColorModeProvider({ children }: { children: ReactNode }) {
    const [colorMode, setColorMode] = useState<ColorMode>(getInitialColorMode);

    useEffect(() => {
        document.documentElement.classList.toggle("dark", colorMode === "dark");
        document.documentElement.classList.toggle("light", colorMode === "light");
        window.localStorage.setItem(STORAGE_KEY, colorMode);

        // The browser re-reads this tag when it changes, so the address bar follows the toggle
        // rather than the operating system's preference. index.html has already set it for the mode
        // we started in; this is what keeps it right after a toggle.
        document.querySelector('meta[name="theme-color"]')?.setAttribute("content", THEME_COLORS[colorMode]);
    }, [colorMode]);

    return (
        <ColorModeContext.Provider
            value={{ colorMode, toggleColorMode: () => setColorMode((c) => (c === "dark" ? "light" : "dark")) }}
        >
            {children}
        </ColorModeContext.Provider>
    );
}
