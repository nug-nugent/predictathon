import { Box, type BoxProps } from "@chakra-ui/react";

type PanelProps = BoxProps & {
    /** Thin top accent stripe (card.accentStripe token) - opt in per-card, not a global default. */
    accent?: boolean;
    /** Shadow + lift on hover - only for clickable/informational cards, not table-heavy panels. */
    hoverLift?: boolean;
};

// Standard "pill" container for content sections - gives dark mode surfaces contrast against
// the near-black page background instead of borderless default boxes blending into it.
//
// Padding is 12px on phones rather than 16px: 32px of it either side of a 360px screen is a lot to
// give up, and several of the tables inside these cards fit or don't fit on exactly those 8px.
export function Panel({ accent = false, hoverLift = false, ...props }: PanelProps) {
    return (
        <Box
            position="relative"
            bg="surface.card"
            borderWidth="1px"
            borderColor="border.card"
            borderRadius="card"
            p={{ base: 3, md: 4 }}
            {...(accent && {
                _before: {
                    content: '""',
                    position: "absolute",
                    top: 0,
                    left: 0,
                    right: 0,
                    height: "3px",
                    bg: "card.accentStripe",
                    borderTopRadius: "card",
                },
            })}
            {...(hoverLift && {
                transition: "box-shadow 0.15s, transform 0.15s",
                _hover: { boxShadow: "cardRaised", transform: "translateY(-2px)" },
            })}
            {...props}
        />
    );
}
