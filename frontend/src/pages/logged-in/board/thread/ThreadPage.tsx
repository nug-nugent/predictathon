import { useEffect, useRef, useState } from "react";
import { useParams } from "react-router";
import { Button, Center, Heading, Spinner, Text, VStack } from "@chakra-ui/react";
import {
    getThread, getMessages, markThreadRead, type MessageThread, type Message, type MessageReaction,
} from "../../../../services/messageboard-service";
import { connectToThread } from "../../../../services/messageboard-hub";
import { ApiError } from "../../../../services/api";
import { MessageItem } from "../../../../components/messageboard/MessageItem";
import { MessageComposer } from "../../../../components/messageboard/MessageComposer";
import { Panel } from "../../../../components/ui/panel";

const PAGE_SIZE = 30;

export function ThreadPage() {
    const { id } = useParams<{ id: string }>();
    const [thread, setThread] = useState<MessageThread | null>(null);
    const [messages, setMessages] = useState<Message[] | null>(null);
    const [hasMore, setHasMore] = useState(true);
    const [loadingOlder, setLoadingOlder] = useState(false);
    const [error, setError] = useState<ApiError | null>(null);

    // Mirrors state into a ref so the SignalR handlers (registered once per connection) always see
    // the latest messages without needing to be re-registered on every state change.
    const messagesRef = useRef<Message[] | null>(null);
    useEffect(() => { messagesRef.current = messages; }, [messages]);

    const reload = () => {
        if (!id) return;
        Promise.all([getThread(id), getMessages(id, undefined, PAGE_SIZE)])
            .then(([threadResult, messagesResult]) => {
                setThread(threadResult);
                setMessages(messagesResult);
                setHasMore(messagesResult.length === PAGE_SIZE);
                markThreadRead(id).catch(() => { /* best-effort - not worth surfacing */ });
            })
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    };

    useEffect(reload, [id]);

    useEffect(() => {
        if (!id) return;

        const appendIfNew = (message: Message) => {
            setMessages((prev) => {
                if (!prev) return prev;
                if (prev.some((m) => m.messageID === message.messageID)) return prev;
                return [...prev, message];
            });
            // A message arrived while this thread is open - keep it marked read rather than
            // letting it reappear as unread on the list page after leaving.
            markThreadRead(id).catch(() => { /* best-effort - not worth surfacing */ });
        };

        const disconnectPromise = connectToThread(id, {
            onNewMessage: appendIfNew,
            onReactionsChanged: (messageId, reactions) => {
                setMessages((prev) =>
                    prev ? prev.map((m) => (m.messageID === messageId ? { ...m, reactions } : m)) : prev);
            },
            onReconnected: () => {
                getMessages(id, undefined, PAGE_SIZE)
                    .then((latest) => {
                        const existingIds = new Set(messagesRef.current?.map((m) => m.messageID));
                        const missing = latest.filter((m) => !existingIds.has(m.messageID));
                        if (missing.length > 0) {
                            setMessages((prev) => (prev ? [...prev, ...missing] : latest));
                        }
                    })
                    .catch(() => { /* best-effort resync */ });
            },
        });

        return () => {
            // Fire-and-forget at unmount - nothing meaningful to do with a failed disconnect here.
            disconnectPromise.then((disconnect) => { void disconnect(); }).catch(() => {});
        };
    }, [id]);

    const loadOlder = async () => {
        if (!id || !messages || messages.length === 0) return;
        setLoadingOlder(true);
        try {
            const older = await getMessages(id, messages[0].messageID, PAGE_SIZE);
            setMessages((prev) => (prev ? [...older, ...prev] : older));
            setHasMore(older.length === PAGE_SIZE);
        } catch {
            // Best-effort - the "Load older" button just stays available to retry.
        } finally {
            setLoadingOlder(false);
        }
    };

    if (!id) {
        return null;
    }

    if (error) {
        return (
            <Center mt={4}>
                <VStack gap={3}>
                    <Text>{error.messages.join(" ")}</Text>
                    <Button onClick={() => { setError(null); reload(); }}>Try again</Button>
                </VStack>
            </Center>
        );
    }

    if (thread === null || messages === null) {
        return (
            <Center mt={4}>
                <Spinner />
            </Center>
        );
    }

    return (
        <VStack align="stretch" gap={2} maxW="container.md" mx="auto">
            <Heading size="lg">{thread.threadSubject}</Heading>

            <Panel accent>
                {hasMore && (
                    <Center mb={2}>
                        <Button size="xs" variant="ghost" loading={loadingOlder} onClick={() => { void loadOlder(); }}>
                            Load older messages
                        </Button>
                    </Center>
                )}

                <VStack align="stretch" gap={0}>
                    {messages.map((message) => (
                        <MessageItem
                            key={message.messageID}
                            message={message}
                            onReactionsChanged={(reactions: MessageReaction[]) =>
                                setMessages((prev) =>
                                    prev ? prev.map((m) => (m.messageID === message.messageID ? { ...m, reactions } : m)) : prev)
                            }
                        />
                    ))}
                </VStack>

                <MessageComposer threadId={id} onPosted={() => { /* the new message arrives via SignalR */ }} />
            </Panel>
        </VStack>
    );
}
