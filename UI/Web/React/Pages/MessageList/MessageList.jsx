import React, { Fragment, useEffect, useLayoutEffect, useRef } from "react";
import useState from "react-usestateref";
import { EmojiPicker } from "../../Modules/emoji-picker";
import { Message } from "../../Components/Message/Message";
import { BoardButton, LoadMoreButton, MessagesContainer, Separator, Title, TitleContainer } from "./style";
import { Icon, icons } from "../../Modules/icons";
import { SignalRManager } from "../../Modules/signalr-client";

export const MessageList = ({ appPath, currentUserId, useSignalR, id, title, messages,
    firstUnreadMessageId, messagesBefore, messagesAfter, customReactionsPath }) => {

    const [messageList, setMessageList, messageListRef] = useState(messages);
    const [olderMessageCount, setOlderMessageCount] = useState(messagesBefore);
    const [newerMessageCount, setNewerMessageCount] = useState(messagesAfter);
    const [newMessagesPosted, setNewMessagesPosted] = useState(false);
    const [isLoadingOlder, setLoadingOlder] = useState(false);
    const [isLoadingNewer, setLoadingNewer] = useState(false);
    const messagesContainerRef = useRef(null);
    const separatorRef = useRef(null);
    const firstMessageDiv = useRef(null);
    const firstMessageScrollPosition = useRef(null);
    
    useEffect(() => {
        useSignalR && new SignalRManager(appPath, currentUserId)
            .registerMessageListeners(id, () => setNewMessagesPosted(true), onReactionsChanged);
        
        // initial scroll position.  setTimeout() required to let the layout settle
        history.scrollRestoration = "manual";
        setTimeout(() => {
            if (firstUnreadMessageId) {
                // scroll to the first unread message separator (if its not there stay at the top)
                separatorRef.current && separatorRef.current.scrollIntoView({ block: "center", behavior: "smooth" });
            } else if (newerMessageCount === 0 && messagesContainerRef.current) {
                // all read, scroll to the bottom
                messagesContainerRef.current.children[messagesContainerRef.current.children.length -1]
                    .scrollIntoView({ behavior: "smooth" });
            }
        }, 100);
    }, []);

    // keep scroll position after loading older messages
    useLayoutEffect(() => {
        if (firstMessageDiv.current && !isLoadingOlder) {
            window.scrollTo(0, firstMessageDiv.current.offsetTop - firstMessageScrollPosition.current);
            firstMessageDiv.current = null;
            firstMessageScrollPosition.current = null;
        }
    }, [isLoadingOlder]);
    
    const onAddReaction = async (messageId, name, url) => {
        callReactionsApi(messageId, "AddReaction", new URLSearchParams({
            ThreadId: id,
            MessageId: messageId,
            Name: name,
            ImageUrl: url
        }));
    };
    
    const onRemoveReaction = (messageId, name) => {
        callReactionsApi(messageId, "RemoveReaction", new URLSearchParams({
            ThreadId: id,
            MessageId: messageId,
            Name: name
        }));
    };
    
    const onReactionsChanged = (messageId) => {
        callReactionsApi(messageId, "GetReactions");
    }
    
    const callReactionsApi = async (messageId, action, requestBody) => {
        const message = messageListRef.current.find((m) => m.id == messageId);
        if (!message) return;
        
        try {
            const response = await fetch(`MessageThreadDetail.aspx?CallBack=${action}${requestBody ? "" : `&MessageId=${messageId}`}`, {
                method: requestBody ? "POST" : "GET",
                body: requestBody
            });
            if (!response.ok) throw new Error(response.statusText);
            message.reactions = await response.json();
            setMessageList([...messageListRef.current]);
        } catch (e) {
            console.log("Reactions API error", e);
        }
    };

    const getOlderMessages = async () => {
        if (isLoadingOlder) return;
        setLoadingOlder(true);

        firstMessageDiv.current = messagesContainerRef.current.children[1];
        firstMessageScrollPosition.current = firstMessageDiv.current.getBoundingClientRect().top;
        
        const firstMessageId = messageListRef.current[0].id;

        try {
            const response = await fetch(`MessageThreadDetail.aspx?CallBack=GetOlderMessages&ThreadId=${id}&BeforeMessageId=${firstMessageId}`);
            if (!response.ok) throw new Error(response.statusText);
            const result = await response.json();
            setMessageList([...result.messages, ...messageListRef.current]);
            setOlderMessageCount(result.messagesBefore);
        } catch (e) {
            console.log("Reactions API error", e);
        }
        setLoadingOlder(false);
    }

    const getNewerMessages = async () => {
        if (isLoadingNewer) return;
        setLoadingNewer(true);

        const lastMessageId = messageListRef.current[messageListRef.current.length - 1].id;

        try {
            const response = await fetch(`MessageThreadDetail.aspx?CallBack=GetNewerMessages&ThreadId=${id}&AfterMessageId=${lastMessageId}`);
            if (!response.ok) throw new Error(response.statusText);
            const result = await response.json();
            setMessageList([...messageListRef.current, ...result.messages]);
            setNewerMessageCount(result.messagesAfter);
            setNewMessagesPosted(false);
        } catch (e) {
            console.log("Reactions API error", e);
        }
        setLoadingNewer(false);
    }

    const [emojiPicker] = useState(new EmojiPicker(onAddReaction, customReactionsPath));

    const newAndNewerMessages = newMessagesPosted && newerMessageCount > 0;
    
    return (
        <>
            <TitleContainer>
                <a href={"Messageboard.aspx"}>
                    <BoardButton>
                        <Icon icon={icons.chevronLeft} />
                        <span>Back</span>
                    </BoardButton>
                </a>
                <Title>{title}</Title>
            </TitleContainer>
            <MessagesContainer ref={messagesContainerRef}>
                {olderMessageCount > 0 && (
                    <LoadMoreButton onClick={getOlderMessages}>
                        {isLoadingOlder ? <Icon icon={icons.loading} pulse /> : <Icon icon={icons.arrowUp} />}
                        <span>{olderMessageCount} older message{olderMessageCount > 1 && "s"}</span>
                    </LoadMoreButton>
                )}

                {messageList.map((m, i) => {
                    return (
                        <Fragment key={m.id}>
                            {i > 0 && firstUnreadMessageId === m.id && (
                                <Separator ref={separatorRef}>new messages</Separator>
                            )}
                            <Message {...m} onOpenEmojiPicker={emojiPicker.toggle.bind(emojiPicker)}
                                onAddReaction={onAddReaction} onRemoveReaction={onRemoveReaction} />
                        </Fragment>
                    );
                })}

                {newerMessageCount > 0 ? (
                    <LoadMoreButton isLoading={isLoadingNewer} onClick={getNewerMessages}>
                        {isLoadingNewer ? <Icon icon={icons.loading} pulse /> : <Icon icon={icons.arrowDown} />}
                        <span>{newerMessageCount}{newAndNewerMessages && "+"} newer message{(newerMessageCount > 1 || newAndNewerMessages) && "s"}</span>
                    </LoadMoreButton>
                ) : (newMessagesPosted ? (
                    <LoadMoreButton isLoading={isLoadingNewer} newMessagesPosted={true} onClick={getNewerMessages}>
                        {isLoadingNewer ? <Icon icon={icons.loading} pulse /> : <Icon icon={icons.arrowDown} bounce style={{ "--fa-bounce-height": "-4px" }} />}
                        <span>new messages</span>
                    </LoadMoreButton>
                ) : <div style={{ height: "38px" }} />)}
            </MessagesContainer>
        </>
    );
};
