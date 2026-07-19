import { useEffect, useState, type ReactNode } from "react";
import { IconButton, type IconButtonProps } from "@chakra-ui/react";
import { Moon, Sun } from "lucide-react";
import { ColorModeContext, STORAGE_KEY, useColorMode, type ColorMode } from "./color-mode-context";

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
    }, [colorMode]);

    return (
        <ColorModeContext.Provider
            value={{ colorMode, toggleColorMode: () => setColorMode((c) => (c === "dark" ? "light" : "dark")) }}
        >
            {children}
        </ColorModeContext.Provider>
    );
}

export function ColorModeButton(props: Omit<IconButtonProps, "aria-label">) {
    const { colorMode, toggleColorMode } = useColorMode();
    const isDark = colorMode === "dark";

    return (
        <IconButton
            aria-label="Toggle color mode" variant="ghost" size="sm" onClick={toggleColorMode}
            // The default ghost hover fill (a flat grey square) looks out of place against the
            // header's solid color - darkening the icon itself instead reads as a much cleaner hover cue.
            _hover={{ bg: "transparent", color: isDark ? "brand.wordmarkFg" : "gray.900" }}
            {...props}
        >
            {isDark ? <Sun size={18} /> : <Moon size={18} />}
        </IconButton>
    );
}
