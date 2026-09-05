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
      shadows: {
        // A crisp, subtle card shadow - used where a page wants more lift than the standard
        // Panel border alone gives it (e.g. the logged-out landing page's login/spotlight cards).
        cardRaised: { value: "0 1px 2px oklch(0.2 0.02 260 / 0.06), 0 12px 32px oklch(0.2 0.02 260 / 0.08)" },
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
        pagination: {
          // Selected page-number fill. Light: header blue (brand.headerBg's #1E4FD1, kept
          // literal since alpha modifiers can't reference a token) with a touch of transparency
          // - a flat oklch fill read as muddy against a white card. Dark: same accent fill the
          // rest of the app uses for "selected" (nav.activeBg/activeFg), left untouched.
          selectedBg: { value: { _light: "rgba(30, 79, 209, 0.85)", _dark: "{colors.nav.activeBg}" } },
          selectedFg: { value: { _light: "#FFFFFF", _dark: "{colors.nav.activeFg}" } },
        },
        nav: {
          fg: { value: { _light: "#6B7280", _dark: "#9A9CA3" } },
          // Solid accent fill - kept for the shared "action" colorPalette (theme.ts below), not just
          // nav items, so don't repurpose its meaning even though NavItem itself now uses activeTint.
          activeBg: { value: { _light: "{colors.brand.accent}", _dark: "{colors.brand.accent}" } },
          activeFg: { value: { _light: "#FFFFFF", _dark: "#0B2A2C" } },
          // Pale accent wash behind the active nav item (left-border + icon-chip treatment) - a tint
          // of brand.accent judged by eye against each mode's background, not a mechanical mix.
          activeTint: { value: { _light: "#E6F4E7", _dark: "#26474D" } },
          sectionLabel: { value: { _light: "#8A8F99", _dark: "#5A5D66" } },
        },
        content: {
          dateHeading: { value: { _light: "#8A8F99", _dark: "#0DE4EE" } },
        },
        surface: {
          sidebar: { value: { _light: "#FFFFFF", _dark: "{colors.bg}" } },
          card: { value: { _light: "#FFFFFF", _dark: "#2A2B30" } },
          // Pale wash marking the one row a table is "about" (e.g. the current team in the Team
          // Detail page's league table) - the same accent tint the active nav item uses.
          highlightRow: { value: { _light: "{colors.nav.activeTint}", _dark: "{colors.nav.activeTint}" } },
          // Inset wash behind a quoted message - the stub above a reply, and the composer's
          // "replying to" chip. Defined explicitly rather than leaning on Chakra's bg.subtle,
          // whose dark default is near-black and reads as a hole punched in the panel rather than
          // an inset. In dark mode it steps *up* from the card, which is what an inset on a dark
          // surface has to do to be seen at all.
          quote: { value: { _light: "#F2F3F6", _dark: "#33353C" } },
          avatarCircle: { value: { _light: "#1E4FD1", _dark: "#0DE4EE" } },
          avatarCircleFg: { value: { _light: "#FFFFFF", _dark: "#1C1D21" } },
        },
        border: {
          hairline: { value: { _light: "#E4E6EB", _dark: "#2E3038" } },
          card: { value: { _light: "#E4E6EB", _dark: "transparent" } },
          // A divider meant to be seen rather than just felt: it separates one person's post from
          // the next in a message thread, where a hairline all but vanishes and the posts run into
          // each other. A step stronger than hairline in both modes, deliberately.
          divider: { value: { _light: "#D8DCE6", _dark: "#3C3E47" } },
        },
        card: {
          accentStripe: { value: { _light: "#2E9B4A", _dark: "{colors.brand.accent}" } },
        },
        // Home page's Recent Form bars. `provisional` marks a match week whose matches haven't all
        // been processed, so its total can still move - functional, like the points scale, not a
        // decorative second blue. Kept deliberately paler than the settled bar in both modes so the
        // difference reads as "not final yet" rather than "a different kind of week".
        form: {
          bar: { value: { _light: "{colors.blue.500}", _dark: "{colors.blue.400}" } },
          barProvisional: { value: { _light: "{colors.blue.200}", _dark: "{colors.blue.700}" } },
        },
        input: {
          bg: { value: { _light: "#FFFFFF", _dark: "#35363C" } },
          border: { value: { _light: "#D8DCE6", _dark: "#45464D" } },
          borderFocus: { value: { _light: "#1E4FD1", _dark: "#4FD9B5" } },
        },
        // Trophy gold - the fallback badge colour for a competition series that names none of its
        // own, and the colour of the win count beside it. Functional like the points scale: it
        // marks an actual competition win, so don't reuse it as a decorative accent. Lightened in
        // dark mode, where the light-mode gold goes muddy against the panel.
        trophy: {
          DEFAULT: { value: { _light: "#B8860B", _dark: "#E8C15A" } },
        },
        status: {
          urgent: { value: { _light: "#D69A1F", _dark: "#E0A93A" } },
          relaxed: { value: { _light: "#2E9B4A", _dark: "#3FCB4A" } },
          // "In play right now" - the broadcast red every score service uses for it. Functional,
          // like the points scale: it marks a match you can still watch unfold, not decoration.
          live: { value: { _light: "#D92D20", _dark: "#FF6257" } },
        },
        // Primary-action colour for buttons/checkboxes/links: the header's blue in light mode,
        // switching to the same bright turquoise (and near-black contrast text) already used for
        // the selected nav item in dark mode - see nav.activeBg/activeFg. Use via colorPalette="action".
        action: {
          contrast: { value: { _light: "white", _dark: "{colors.nav.activeFg}" } },
          fg: { value: { _light: "oklch(0.4 0.15 265)", _dark: "{colors.nav.activeBg}" } },
          subtle: { value: { _light: "{colors.blue.100}", _dark: "{colors.cyan.900}" } },
          muted: { value: { _light: "{colors.blue.200}", _dark: "{colors.cyan.800}" } },
          emphasized: { value: { _light: "{colors.blue.300}", _dark: "{colors.cyan.700}" } },
          solid: { value: { _light: "oklch(0.4 0.15 265)", _dark: "{colors.nav.activeBg}" } },
          focusRing: { value: { _light: "oklch(0.4 0.15 265)", _dark: "{colors.nav.activeBg}" } },
          border: { value: { _light: "oklch(0.4 0.15 265)", _dark: "{colors.cyan.400}" } },
        },
      },
    },
  },
});
