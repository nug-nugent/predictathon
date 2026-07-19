import { Box, type BoxProps } from "@chakra-ui/react";

// Standard "pill" container for content sections - gives dark mode surfaces contrast against
// the near-black page background instead of borderless default boxes blending into it.
export function Panel(props: BoxProps) {
    return <Box bg="surface.card" borderWidth="1px" borderColor="border.card" borderRadius="card" p={4} {...props} />;
}
