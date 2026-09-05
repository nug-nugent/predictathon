import { Fragment, useCallback, useEffect, useRef, useState } from "react";
import { useParams } from "react-router";
import { Button, Center, Heading, Spinner, Text, VStack } from "@chakra-ui/react";
import { ArrowDown, ArrowUp } from "lucide-react";
import {
    getThread, getMessages, markThreadRead,
    type MessageThread, type MessageThreadPage, type Message, type MessageReaction,
} from "../../../../services/messageboard-service";
import { connectToThread } from "../../../../services/messageboard-hub";
import { ApiError } from "../../../../services/api";
import { MessageItem } from "../../../../components/messageboard/MessageItem";
import { MessageComposer } from "../../../../components/messageboard/MessageComposer";
import { UnreadSeparator } from "../../../../components/messageboard/UnreadSeparator";
import { Panel } from "../../../../components/ui/panel";

const PAGE_SIZE = 30;

// How many older pages jumping to a reply's parent will pull in before giving up. A parent is
// nearly always a few messages up (people reply to what they have just read), so this is a
// backstop against walking an entire multi-year thread, not the expected path.
const MAX_JUMP_PAGES = 5;

// How long a jumped-to message stays washed in the highlight tint.
const HIGHLIGHT_MS = 1600;

/// Where the thread should put the reader once its first window has rendered: on the unread
/// boundary if there is one, otherwise at the newest message.
type Anchor = "unread" | "bottom";

export function ThreadPage() {
    const { id } = useParams<{ id: string }>();
    const [thread, setThread] = useState<MessageThread | null>(null);
    const [messages, setMessages] = useState<Message[] | null>(null);
    const [messagesBefore, setMessagesBefore] = useState(0);
    const [messagesAfter, setMessagesAfter] = useState(0);
    const [firstUnreadMessageId, setFirstUnreadMessageId] = useState<string | null>(null);
    const [loadingOlder, setLoadingOlder] = useState(false);
    const [loadingNewer, setLoadingNewer] = useState(false);
    const [error, setError] = useState<ApiError | null>(null);
    const [replyTo, setReplyTo] = useState<Message | null>(null);
    const [highlightedId, setHighlightedId] = useState<string | null>(null);

    // Mirrors state into refs so the SignalR handlers (registered once per connection) always see
    // the latest values without needing to be re-registered on every state change.
    const messagesRef = useRef<Message[] | null>(null);
    useEffect(() => { messagesRef.current = messages; }, [messages]);
    const messagesAfterRef = useRef(0);
    useEffect(() => { messagesAfterRef.current = messagesAfter; }, [messagesAfter]);

    // One entry per rendered message, so jumping to a parent can scroll and focus its row.
    const messageRefs = useRef(new Map<string, HTMLDivElement>());
    const registerMessageRef = useCallback((messageId: string, node: HTMLDivElement | null) => {
        if (node) {
            messageRefs.current.set(messageId, node);
        } else {
            messageRefs.current.delete(messageId);
        }
    }, []);

    const separatorRef = useRef<HTMLDivElement>(null);

    // Set when a fetch lands that should reposition the reader, and consumed by the effect below
    // once React has painted the new messages. A ref rather than state: it isn't rendered, and it
    // must not itself trigger a render.
    const pendingAnchor = useRef<Anchor | null>(null);

    // A reply target belongs to the thread it was picked in, so it's derived rather than cleared
    // on navigation: routing to another thread reuses this component, and deriving means there is
    // no window in which the composer offers to reply to a message from the thread just left.
    const activeReplyTo = replyTo?.messageThreadID === id ? replyTo : null;

    const highlightTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);
    useEffect(() => () => {
        if (highlightTimeout.current) clearTimeout(highlightTimeout.current);
    }, []);

    // The browser would otherwise restore the previous scroll offset on a back-navigation, fighting
    // the anchor below for control of where the thread opens. Restored on unmount so the rest of
    // the app keeps normal restoration.
    useEffect(() => {
        const previous = history.scrollRestoration;
        history.scrollRestoration = "manual";
        return () => { history.scrollRestoration = previous; };
    }, []);

    const applyPage = (page: MessageThreadPage) => {
        setMessages(page.messages);
        setMessagesBefore(page.messagesBefore);
        setMessagesAfter(page.messagesAfter);
        setFirstUnreadMessageId(page.firstUnreadMessageID);
        pendingAnchor.current = page.firstUnreadMessageID ? "unread" : "bottom";
    };

    const reload = () => {
        if (!id) return;
        Promise.all([getThread(id), getMessages(id, {}, PAGE_SIZE)])
            .then(([threadResult, page]) => {
                setThread(threadResult);
                applyPage(page);
                // Marked read only after the window has been worked out - the server needs the old
                // read position to find the unread boundary this load is anchored on.
                markThreadRead(id).catch(() => { /* best-effort - not worth surfacing */ });
            })
            .catch((err) => setError(err instanceof ApiError ? err : new ApiError(0, ["Something went wrong."])));
    };

    useEffect(reload, [id]);

    // Puts the reader where they left off, once the messages are on the page. The timeout lets
    // layout settle first: message images have no intrinsic size until they load, so scrolling in
    // the same frame would aim at coordinates that are about to move.
    useEffect(() => {
        if (!messages || messages.length === 0 || !pendingAnchor.current) return;

        const anchor = pendingAnchor.current;
        pendingAnchor.current = null;

        const timer = setTimeout(() => {
            if (anchor === "unread" && separatorRef.current) {
                separatorRef.current.scrollIntoView({ block: "center", behavior: "smooth" });
                return;
            }

            // The end of the document rather than the last message, so the composer lands on
            // screen too: someone who arrives already caught up is usually there to reply.
            window.scrollTo({ top: document.documentElement.scrollHeight, behavior: "smooth" });
        }, 0);

        return () => clearTimeout(timer);
    }, [messages]);

    /// Appends a forward page, ignoring anything already held. Growing the list downwards doesn't
    /// move what's on screen, so unlike prepending this needs no scroll correction.
    const appendPage = useCallback((page: MessageThreadPage) => {
        if (page.messages.length > 0) {
            setMessages((prev) => {
                if (!prev) return page.messages;
                const held = new Set(prev.map((m) => m.messageID));
                return [...prev, ...page.messages.filter((m) => !held.has(m.messageID))];
            });
        }
        setMessagesAfter(page.messagesAfter);
    }, []);

    useEffect(() => {
        if (!id) return;

        const receive = (message: Message) => {
            // Only append when the window is already at the live end. Further back, appending
            // would splice the newest message onto an older page with a hole in between - so the
            // arrival just bumps the "newer messages" count instead, and the reader chooses when
            // to catch up.
            if (messagesAfterRef.current > 0) {
                setMessagesAfter((n) => n + 1);
                return;
            }

            setMessages((prev) => {
                if (!prev) return prev;
                if (prev.some((m) => m.messageID === message.messageID)) return prev;
                return [...prev, message];
            });
            pendingAnchor.current = "bottom";
            // A message arrived while this thread is open - keep it marked read rather than
            // letting it reappear as unread on the list page after leaving.
            markThreadRead(id).catch(() => { /* best-effort - not worth surfacing */ });
        };

        const disconnectPromise = connectToThread(id, {
            onNewMessage: receive,
            onReactionsChanged: (messageId, reactions) => {
                setMessages((prev) =>
                    prev ? prev.map((m) => (m.messageID === messageId ? { ...m, reactions } : m)) : prev);
            },
            onReconnected: () => {
                const current = messagesRef.current;
                if (!current || current.length === 0) return;

                // Fills the gap missed while disconnected from the newest message held, rather than
                // refetching the latest page - which would strand the reader if they were paged
                // back through the thread when the connection dropped.
                getMessages(id, { after: current[current.length - 1].messageID }, PAGE_SIZE)
                    .then((page) => {
                        appendPage(page);
                    })
                    .catch(() => { /* best-effort resync */ });
            },
        });

        return () => {
            // Fire-and-forget at unmount - nothing meaningful to do with a failed disconnect here.
            disconnectPromise.then((disconnect) => { void disconnect(); }).catch(() => {});
        };
    }, [id, appendPage]);

    /// Fetches one page of older messages and prepends it, returning the page (empty if there was
    /// nothing left, or the request failed).
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
            const page = await getMessages(id, { before: current[0].messageID }, PAGE_SIZE);
            if (page.messages.length > 0) {
                setMessages((prev) => (prev ? [...page.messages, ...prev] : page.messages));
            }
            setMessagesBefore(page.messagesBefore);

            if (page.messages.length > 0) {
                // After paint, so scrollHeight reflects the rows that were just added.
                requestAnimationFrame(() => {
                    window.scrollTo({ top: scrollBefore + (document.documentElement.scrollHeight - heightBefore) });
                });
            }

            return page.messages;
        } catch {
            // Best-effort - the "older messages" button just stays available to retry.
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

    const loadNewer = async () => {
        const current = messagesRef.current;
        if (!id || !current || current.length === 0) return;

        setLoadingNewer(true);
        try {
            const page = await getMessages(id, { after: current[current.length - 1].messageID }, PAGE_SIZE);
            appendPage(page);
            if (page.messagesAfter === 0) {
                markThreadRead(id).catch(() => { /* best-effort - not worth surfacing */ });
            }
        } catch {
            // Best-effort - the "newer messages" button stays available to retry.
        } finally {
            setLoadingNewer(false);
        }
    };

    /// Jumps to the newest page. Used after posting from a window that was paged back through the
    /// thread, where the reader's own message would otherwise land somewhere they can't see.
    const goToLatest = useCallback(async () => {
        if (!id) return;

        setLoadingNewer(true);
        try {
            const page = await getMessages(id, {}, PAGE_SIZE);
            setMessages(page.messages);
            setMessagesBefore(page.messagesBefore);
            setMessagesAfter(page.messagesAfter);
            // Deliberately at the end now, so any unread boundary has been read past.
            setFirstUnreadMessageId(null);
            pendingAnchor.current = "bottom";
            markThreadRead(id).catch(() => { /* best-effort - not worth surfacing */ });
        } catch {
            // Best-effort - the message did post, it just isn't in view.
        } finally {
            setLoadingNewer(false);
        }
    }, [id]);

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
                if (older.length === 0) return;
            }
        } finally {
            setLoadingOlder(false);
        }
    }, [loadOlderPage, revealMessage]);

    const handlePosted = () => {
        if (messagesAfterRef.current > 0) {
            void goToLatest();
            return;
        }
        // Otherwise the message arrives via SignalR, and the anchor effect scrolls to it.
        pendingAnchor.current = "bottom";
    };

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
                {messagesBefore > 0 && (
                    <Center mb={2}>
                        <Button size="xs" variant="ghost" loading={loadingOlder} onClick={() => { void loadOlder(); }}>
                            <ArrowUp size={14} />
                            {messagesBefore} older message{messagesBefore === 1 ? "" : "s"}
                        </Button>
                    </Center>
                )}

                <VStack align="stretch" gap={0}>
                    {messages.map((message, index) => (
                        <Fragment key={message.messageID}>
                            {/* Not on the very first message loaded: a boundary at the top of the
                                window separates nothing from anything. */}
                            {index > 0 && message.messageID === firstUnreadMessageId && (
                                <UnreadSeparator ref={separatorRef} />
                            )}
                            <MessageItem
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
                        </Fragment>
                    ))}
                </VStack>

                {messagesAfter > 0 && (
                    <Center mt={2}>
                        <Button size="xs" variant="ghost" loading={loadingNewer} onClick={() => { void loadNewer(); }}>
                            <ArrowDown size={14} />
                            {messagesAfter} newer message{messagesAfter === 1 ? "" : "s"}
                        </Button>
                    </Center>
                )}

                <MessageComposer
                    threadId={id}
                    replyTo={activeReplyTo}
                    onClearReply={() => setReplyTo(null)}
                    onPosted={handlePosted}
                />
            </Panel>
        </VStack>
    );
}
