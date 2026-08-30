import { Box, HStack, Text } from "@chakra-ui/react";

// The dot pulses to say "this is updating", but the word LIVE carries the meaning on its own -
// colour and motion are never the only signal, and the animation is dropped for anyone who has
// asked their system to reduce motion.
export function LiveBadge({ size = "sm" }: { size?: "xs" | "sm" }) {
    return (
        <HStack gap={1.5} flexShrink={0}>
            <Box
                boxSize="8px"
                borderRadius="full"
                bg="status.live"
                animationName="pulse"
                animationDuration="1.8s"
                animationIterationCount="infinite"
                css={{ "@media (prefers-reduced-motion: reduce)": { animationName: "none" } }}
            />
            <Text fontSize={size === "xs" ? "0.65rem" : "xs"} fontWeight="bold" letterSpacing="wide" color="status.live">
                LIVE
            </Text>
        </HStack>
    );
}
