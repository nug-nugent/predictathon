import { createSystem, defaultConfig } from "@chakra-ui/react"

export const predictTheme = createSystem(defaultConfig, {
  theme: {
    tokens: {
      fonts: {
        heading: { value: `'Manrope', -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif` },
        body: { value: `'Inter', -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif` },
      },
      radii: {
        card: { value: "12px" },
      },
      colors: {
        points: {
          0: { value: "#EE0000" },
          1: { value: "#FF6600" },
          2: { value: "#888800" },
          3: { value: "#5cb85c" }
        },
      },
    },
    recipes: {
      input: {
        variants: {
          variant: {
            outline: {
              borderColor: "input.border",
              _focusVisible: { borderColor: "input.borderFocus" },
            },
          },
        },
      },
      textarea: {
        variants: {
          variant: {
            outline: {
              borderColor: "input.border",
              _focusVisible: { borderColor: "input.borderFocus" },
            },
          },
        },
      },
    },
    semanticTokens: {
      colors: {
        bg: {
          DEFAULT: { value: { _light: "#FAFAFB", _dark: "#1C1D21" } },
          panel: { value: { _light: "#FFFFFF", _dark: "#2A2B30" } },
        },
        brand: {
          headerBg: { value: { _light: "#1E4FD1", _dark: "{colors.bg}" } },
          headerBorder: { value: { _light: "transparent", _dark: "#2E3038" } },
          wordmarkFg: { value: { _light: "#FFFFFF", _dark: "#F5F6F8" } },
          subtitleFg: { value: { _light: "#CCCCCC", _dark: "#0DE4EE" } },
          accent: { value: { _light: "#2E9B4A", _dark: "#0DE4EE" } },
        },
        nav: {
          fg: { value: { _light: "#6B7280", _dark: "#9A9CA3" } },
          activeBg: { value: { _light: "{colors.brand.accent}", _dark: "{colors.brand.accent}" } },
          activeFg: { value: { _light: "#FFFFFF", _dark: "#0B2A2C" } },
          sectionLabel: { value: { _light: "#8A8F99", _dark: "#5A5D66" } },
        },
        content: {
          dateHeading: { value: { _light: "#8A8F99", _dark: "#0DE4EE" } },
        },
        surface: {
          sidebar: { value: { _light: "#FFFFFF", _dark: "{colors.bg}" } },
          card: { value: { _light: "#FFFFFF", _dark: "#2A2B30" } },
          avatarChip: { value: { _light: "#F0F1F3", _dark: "#2A2B30" } },
          avatarCircle: { value: { _light: "#1E4FD1", _dark: "#0DE4EE" } },
          avatarCircleFg: { value: { _light: "#FFFFFF", _dark: "#1C1D21" } },
        },
        border: {
          hairline: { value: { _light: "#E4E6EB", _dark: "#2E3038" } },
          card: { value: { _light: "#E4E6EB", _dark: "transparent" } },
        },
        card: {
          accentStripe: { value: { _light: "#2E9B4A", _dark: "transparent" } },
        },
        input: {
          bg: { value: { _light: "#FFFFFF", _dark: "#35363C" } },
          border: { value: { _light: "#D8DCE6", _dark: "#45464D" } },
          borderFocus: { value: { _light: "#1E4FD1", _dark: "#4FD9B5" } },
        },
        status: {
          urgent: { value: { _light: "#D69A1F", _dark: "#E0A93A" } },
          relaxed: { value: { _light: "#2E9B4A", _dark: "#3FCB4A" } },
        },
      },
    },
  },
});
