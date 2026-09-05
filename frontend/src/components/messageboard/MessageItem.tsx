import { forwardRef } from "react";
import { Avatar, Box, Button, HStack, Image, Link, Stack, Text, VStack } from "@chakra-ui/react";
import { Reply } from "lucide-react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { Message, MessageReaction } from "../../services/messageboard-service";
import { ReactionBar } from "./ReactionBar";
import { ReplyQuote } from "./ReplyQuote";
import { TrophyStamp } from "../trophies/TrophyStamp";
import { formatDateTime } from "../../utils/formatDateTime";

const markdownComponents = {
    p: ({ children }: { children?: React.ReactNode }) => <Text mb={2} _last={{ mb: 0 }}>{children}</Text>,
    a: ({ href, children }: { href?: string; children?: React.ReactNode }) => (
        <Link href={href} target="_blank" rel="noopener noreferrer" colorPalette="action">{children}</Link>
    ),
};

/// Forwards a ref onto the message's outer row so the thread can scroll to it and move focus there
/// when a reply's quoted stub is used to jump back to it.
export const MessageItem = forwardRef<HTMLDivElement, {
    message: Message;
    onReactionsChanged: (reactions: MessageReaction[]) => void;
    onReply: (message: Message) => void;
    onJumpToReplyParent: (messageId: string) => void;
    /// Briefly true after being jumped to, to wash the row in the highlight tint.
    highlighted?: boolean;
}>(function MessageItem({ message, onReactionsChanged, onReply, onJumpToReplyParent, highlighted }, ref) {
    return (
        <HStack
            ref={ref}
            // Focusable only programmatically: jumping to a message moves focus here so the jump is
            // announced, rather than the highlight tint being the only cue that anything happened.
            tabIndex={-1}
            align="start"
            gap={3}
            py={4}
            px={2}
            mx={-2}
            rounded="md"
            borderTopWidth="1px"
            borderColor="border.divider"
            _first={{ borderTopWidth: 0 }}
            bg={highlighted ? "surface.highlightRow" : undefined}
            transition="background-color 0.6s ease-out"
            _focusVisible={{ outline: "2px solid", outlineColor: "input.borderFocus", outlineOffset: "-2px" }}
        >
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

                {message.replyTo && (
                    <ReplyQuote replyTo={message.replyTo} onJump={() => onJumpToReplyParent(message.replyTo!.messageID)} />
                )}

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

                <HStack gap={1} wrap="wrap">
                    <ReactionBar messageId={message.messageID} reactions={message.reactions} onChanged={onReactionsChanged} />
                    <Button
                        size="2xs"
                        variant="ghost"
                        color="fg.muted"
                        onClick={() => onReply(message)}
                        aria-label={`Reply to ${message.postedByUsername}'s message`}
                    >
                        <Reply size={12} /> Reply
                    </Button>
                </HStack>
            </VStack>
        </HStack>
    );
});
