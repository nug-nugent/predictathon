import { Avatar, Box, HStack, Image, Link, Text, VStack } from "@chakra-ui/react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { Message, MessageReaction } from "../../services/messageboard-service";
import { ReactionBar } from "./ReactionBar";

const markdownComponents = {
    p: ({ children }: { children?: React.ReactNode }) => <Text mb={2} _last={{ mb: 0 }}>{children}</Text>,
    a: ({ href, children }: { href?: string; children?: React.ReactNode }) => (
        <Link href={href} target="_blank" rel="noopener noreferrer" colorPalette="blue">{children}</Link>
    ),
};

export function MessageItem({ message, onReactionsChanged }: {
    message: Message;
    onReactionsChanged: (reactions: MessageReaction[]) => void;
}) {
    return (
        <HStack align="start" gap={3} py={3} borderTopWidth="1px" _first={{ borderTopWidth: 0 }}>
            <Avatar.Root size="sm">
                <Avatar.Image src={message.postedByAvatarUrl ?? undefined} />
                <Avatar.Fallback name={message.postedByUsername} />
            </Avatar.Root>
            <VStack align="start" gap={1} flex="1" minW={0}>
                <HStack gap={2}>
                    <Text fontWeight="bold">{message.postedByUsername}</Text>
                    <Text fontSize="xs" color="fg.muted">{new Date(message.messageDateTime).toLocaleString()}</Text>
                    <Text fontSize="xs" color="fg.muted">&middot; post #{message.posterTotalMessageboardPosts}</Text>
                </HStack>

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
                        <iframe
                            width="100%"
                            height="100%"
                            src={`https://www.youtube.com/embed/${message.youTubeVideoID}`}
                            title="YouTube video"
                            frameBorder="0"
                            allowFullScreen
                        />
                    </Box>
                )}

                <ReactionBar messageId={message.messageID} reactions={message.reactions} onChanged={onReactionsChanged} />
            </VStack>
        </HStack>
    );
}
