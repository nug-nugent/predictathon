import { Avatar, Box, HStack, Image, Link, Stack, Text, VStack } from "@chakra-ui/react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { Message, MessageReaction } from "../../services/messageboard-service";
import { ReactionBar } from "./ReactionBar";
import { TrophyStamp } from "../trophies/TrophyStamp";
import { formatDateTime } from "../../utils/formatDateTime";

const markdownComponents = {
    p: ({ children }: { children?: React.ReactNode }) => <Text mb={2} _last={{ mb: 0 }}>{children}</Text>,
    a: ({ href, children }: { href?: string; children?: React.ReactNode }) => (
        <Link href={href} target="_blank" rel="noopener noreferrer" colorPalette="action">{children}</Link>
    ),
};

export function MessageItem({ message, onReactionsChanged }: {
    message: Message;
    onReactionsChanged: (reactions: MessageReaction[]) => void;
}) {
    return (
        <HStack align="start" gap={3} py={4} borderTopWidth="1px" borderColor="border.divider" _first={{ borderTopWidth: 0 }}>
            <Avatar.Root size="sm">
                <Avatar.Image src={message.postedByAvatarUrl ?? undefined} />
                <Avatar.Fallback name={message.postedByUsername} />
            </Avatar.Root>
            <VStack align="start" gap={1} flex="1" minW={0}>
                {/* One line on a normal screen, but there isn't room for a name, a trophy stamp
                    and a timestamp across a phone: everything is flex and shrinks, so the date and
                    post counter each get crushed into two crumpled lines. Below sm the metadata
                    drops under the name instead, and nowrap stops either value breaking mid-value
                    wherever it ends up. */}
                <Stack
                    direction={{ base: "column", sm: "row" }}
                    gap={{ base: 0, sm: 2 }}
                    align={{ base: "start", sm: "center" }}
                    w="100%"
                >
                    <HStack gap={2} minW={0} maxW="100%">
                        <Text fontWeight="bold" truncate>{message.postedByUsername}</Text>
                        <TrophyStamp trophies={message.posterTrophies} interactive ownerName={message.postedByUsername} />
                    </HStack>
                    <HStack gap={2} flexShrink={0} fontSize="xs" color="fg.muted">
                        <Text whiteSpace="nowrap">{formatDateTime(message.messageDateTime)}</Text>
                        <Text whiteSpace="nowrap">&middot; post #{message.posterTotalMessageboardPosts}</Text>
                    </HStack>
                </Stack>

                {message.messageContent && (
                    <Box fontSize="sm">
                        <ReactMarkdown remarkPlugins={[remarkGfm]} components={markdownComponents}>
                            {message.messageContent}
                        </ReactMarkdown>
                    </Box>
                )}

                {message.imageUrl && (
                    <Image src={message.imageUrl} maxH="320px" rounded="md" alt="" />
                )}

                {message.youTubeVideoID && (
                    <Box w="100%" maxW="480px" aspectRatio={16 / 9}>
                        {/* youtube-nocookie: no tracking cookies until playback starts. */}
                        <iframe
                            width="100%"
                            height="100%"
                            style={{ border: 0 }}
                            src={`https://www.youtube-nocookie.com/embed/${message.youTubeVideoID}`}
                            title="YouTube video"
                            allowFullScreen
                        />
                    </Box>
                )}

                <ReactionBar messageId={message.messageID} reactions={message.reactions} onChanged={onReactionsChanged} />
            </VStack>
        </HStack>
    );
}
