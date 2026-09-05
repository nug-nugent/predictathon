import { useCallback, useEffect, useRef, useState } from "react";
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

// How many older pages jumping to a reply's parent will pull in before giving up. A parent is
// nearly always a few messages up (people reply to what they have just read), so this is a
// backstop against walking an entire multi-year thread, not the expected path.
const MAX_JUMP_PAGES = 5;

// How long a jumped-to message stays washed in the highlight tint.
const HIGHLIGHT_MS = 1600;

export function ThreadPage() {
    const { id } = useParams<{ id: string }>();
    const [thread, setThread] = useState<MessageThread | null>(null);
    const [messages, setMessages] = useState<Message[] | null>(null);
    const [hasMore, setHasMore] = useState(true);
    const [loadingOlder, setLoadingOlder] = useState(false);
    const [error, setError] = useState<ApiError | null>(null);
    const [replyTo, setReplyTo] = useState<Message | null>(null);
    const [highlightedId, setHighlightedId] = useState<string | null>(null);

    // Mirrors state into a ref so the SignalR handlers (registered once per connection) always see
    // the latest messages without needing to be re-registered on every state change.
    const messagesRef = useRef<Message[] | null>(null);
    useEffect(() => { messagesRef.current = messages; }, [messages]);

    // One entry per rendered message, so jumping to a parent can scroll and focus its row.
    const messageRefs = useRef(new Map<string, HTMLDivElement>());
    const registerMessageRef = useCallback((messageId: string, node: HTMLDivElement | null) => {
        if (node) {
            messageRefs.current.set(messageId, node);
        } else {
            messageRefs.current.delete(messageId);
        }
    }, []);

    // A reply target belongs to the thread it was picked in, so it's derived rather than cleared
    // on navigation: routing to another thread reuses this component, and deriving means there is
    // no window in which the composer offers to reply to a message from the thread just left.
    const activeReplyTo = replyTo?.messageThreadID === id ? replyTo : null;

    const highlightTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);
    useEffect(() => () => {
        if (highlightTimeout.current) clearTimeout(highlightTimeout.current);
    }, []);

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

    /// Fetches one page of older messages and prepends it, returning what was added (empty if
    /// there was nothing left, or the request failed).
    ///
    /// Prepending pushes everything already on screen down by the height of what arrived, so the
    /// document's scroll position is corrected by that same amount afterwards - otherwise the
    /// message you were reading jumps away the moment older ones load in above it.
    const loadOlderPage = useCallback(async (): Promise<Message[]> => {
        const current = messagesRef.current;
        if (!id || !current || current.length === 0) return [];

        const heightBefore = document.documentElement.scrollHeight;
        const scrollBefore = window.scrollY;

        try {
            const older = await getMessages(id, current[0].messageID, PAGE_SIZE);
            if (older.length > 0) {
                setMessages((prev) => (prev ? [...older, ...prev] : older));
            }
            setHasMore(older.length === PAGE_SIZE);

            if (older.length > 0) {
                // After paint, so scrollHeight reflects the rows that were just added.
                requestAnimationFrame(() => {
                    window.scrollTo({ top: scrollBefore + (document.documentElement.scrollHeight - heightBefore) });
                });
            }

            return older;
        } catch {
            // Best-effort - the "Load older" button just stays available to retry.
            return [];
        }
    }, [id]);

    const loadOlder = async () => {
        setLoadingOlder(true);
        try {
            await loadOlderPage();
        } finally {
            setLoadingOlder(false);
        }
    };

    /// Scrolls a message into view and briefly highlights it, moving focus there so the jump is
    /// perceivable without relying on the colour wash alone.
    const revealMessage = useCallback((messageId: string) => {
        const node = messageRefs.current.get(messageId);
        if (!node) return false;

        node.scrollIntoView({ block: "center", behavior: "smooth" });
        node.focus({ preventScroll: true });

        setHighlightedId(messageId);
        if (highlightTimeout.current) clearTimeout(highlightTimeout.current);
        highlightTimeout.current = setTimeout(() => setHighlightedId(null), HIGHLIGHT_MS);

        return true;
    }, []);

    /// Jumps to the message a reply is quoting. Usually it is already on screen; when it isn't,
    /// older pages are pulled in until it turns up or there is nothing older left to load.
    const jumpToMessage = useCallback(async (messageId: string) => {
        if (revealMessage(messageId)) return;

        setLoadingOlder(true);
        try {
            for (let page = 0; page < MAX_JUMP_PAGES; page++) {
                const older = await loadOlderPage();
                if (older.some((m) => m.messageID === messageId)) {
                    // The row only exists after React has painted the newly prepended messages.
                    requestAnimationFrame(() => revealMessage(messageId));
                    return;
                }
                if (older.length < PAGE_SIZE) return;
            }
        } finally {
            setLoadingOlder(false);
        }
    }, [loadOlderPage, revealMessage]);

    if (!id) {
        return null;
    }

    if (error) {
        return (
            <Center mt={4}>
                <VStack gap={3}>
                    <Text>{error.messages.join(" ")}</Text>
                    <Button onClick={() => { setError(null); reload(); }}>Try Again</Button>
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
                            Load Older Messages
                        </Button>
                    </Center>
                )}

                <VStack align="stretch" gap={0}>
                    {messages.map((message) => (
                        <MessageItem
                            key={message.messageID}
                            ref={(node) => registerMessageRef(message.messageID, node)}
                            message={message}
                            highlighted={highlightedId === message.messageID}
                            onReply={setReplyTo}
                            onJumpToReplyParent={(messageId) => { void jumpToMessage(messageId); }}
                            onReactionsChanged={(reactions: MessageReaction[]) =>
                                setMessages((prev) =>
                                    prev ? prev.map((m) => (m.messageID === message.messageID ? { ...m, reactions } : m)) : prev)
                            }
                        />
                    ))}
                </VStack>

                <MessageComposer
                    threadId={id}
                    replyTo={activeReplyTo}
                    onClearReply={() => setReplyTo(null)}
                    onPosted={() => { /* the new message arrives via SignalR */ }}
                />
            </Panel>
        </VStack>
    );
}
