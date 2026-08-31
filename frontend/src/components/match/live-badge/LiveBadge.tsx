import { Box, HStack, Text } from "@chakra-ui/react";

// Pulses to say "this is updating". Never the only signal that something is live - it sits beside
// the word either way - and the animation is dropped for anyone who has asked their system to
// reduce motion.
export function LivePulseDot({ boxSize = "8px" }: { boxSize?: string }) {
    return (
        <Box
            boxSize={boxSize}
            borderRadius="full"
            bg="status.live"
            flexShrink={0}
            animationName="pulse"
            animationDuration="1.8s"
            animationIterationCount="infinite"
            css={{ "@media (prefers-reduced-motion: reduce)": { animationName: "none" } }}
        />
    );
}

// The dot plus the word, for places that aren't already labelled - the Live page's focused match,
// and the Home card's link through to it.
export function LiveBadge({ size = "sm" }: { size?: "xs" | "sm" }) {
    return (
        <HStack gap={1.5} flexShrink={0}>
            <LivePulseDot />
            <Text fontSize={size === "xs" ? "0.65rem" : "xs"} fontWeight="bold" letterSpacing="wide" color="status.live">
                LIVE
            </Text>
        </HStack>
    );
}
