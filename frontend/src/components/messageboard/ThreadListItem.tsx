import { Badge, Box, HStack, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import type { MessageThreadSummary } from "../../services/messageboard-service";

export function ThreadListItem({ thread, striped = false }: { thread: MessageThreadSummary; striped?: boolean }) {
    return (
        <RouterLink to={`/board/${thread.messageThreadID}`}>
            <HStack
                justify="space-between"
                align="start"
                gap={4}
                py={3}
                px={2}
                borderTopWidth="1px"
                _first={{ borderTopWidth: 0 }}
                // bg.subtle for the stripe, bg.muted (a step stronger) for hover - so hovering a
                // striped row still reads as a distinct highlight rather than disappearing into it.
                bg={striped ? "bg.subtle" : undefined}
                _hover={{ bg: "bg.muted" }}
                rounded="md"
            >
                <VStack align="start" gap={0} flex="1" minW={0}>
                    <HStack>
                        {thread.unread && <Box boxSize="8px" rounded="full" bg="blue.500" flexShrink={0} aria-label="Unread" />}
                        <Text fontWeight={thread.unread ? "bold" : "normal"} truncate>{thread.threadSubject}</Text>
                        {thread.hiddenFromPublic && <Badge colorPalette="orange" size="sm">Hidden</Badge>}
                    </HStack>
                    <Text fontSize="sm" color="fg.muted">
                        Started by {thread.startedByUsername} &middot; {thread.messageCount} {thread.messageCount === 1 ? "post" : "posts"}
                    </Text>
                </VStack>
                <VStack align="end" gap={0} flexShrink={0}>
                    <Text fontSize="sm" color="fg.muted">{new Date(thread.lastMessageDateTime).toLocaleString()}</Text>
                    <Text fontSize="sm" color="fg.muted">by {thread.lastMessageByUsername}</Text>
                </VStack>
            </HStack>
        </RouterLink>
    );
}
