import { forwardRef } from "react";
import { Box, HStack, Text } from "@chakra-ui/react";

/// Marks where the reader got to last time, between the last message they've seen and the first
/// they haven't. The thread opens scrolled to this, so it also acts as the landing point.
///
/// Forwards a ref onto the outer row so the thread can scroll it into view once the messages
/// around it have rendered.
export const UnreadSeparator = forwardRef<HTMLDivElement, object>(function UnreadSeparator(_props, ref) {
    return (
        <HStack ref={ref} gap={3} py={2} aria-label="New messages below">
            <Box flex="1" h="1px" bg="fg.error" opacity={0.5} />
            <Text
                fontSize="xs"
                fontWeight="bold"
                color="fg.error"
                textTransform="uppercase"
                letterSpacing="wide"
                flexShrink={0}
            >
                New messages
            </Text>
            <Box flex="1" h="1px" bg="fg.error" opacity={0.5} />
        </HStack>
    );
});
