import React from "react";
import ReactDOM from "react-dom";
import { MessageList } from "../Pages/MessageList/MessageList";

document.addEventListener("DOMContentLoaded", function(event) { 
    ReactDOM.render(
        <MessageList appPath={window.appPath} currentUserId={window.currentUserId}
        liveUpdatesEnabled={true} messagesLoadedTime={window.messagesLoadedTime}
        id={window.threadId} title={window.threadTitle} messages={window.threadMessages}
        firstUnreadMessageId={window.firstUnreadMessageId}
        messagesBefore={window.messagesBefore} messagesAfter={window.messagesAfter}
        customReactionsPath="../../Images/Message/Reactions" />,
        document.getElementById("message-list")
    );
});
