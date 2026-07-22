import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { getAccessToken, getHubUrl } from "./api";
import type { Message, MessageReaction } from "./messageboard-service";

export type MessageboardHubHandlers = {
    onNewMessage: (message: Message) => void;
    onReactionsChanged: (messageId: string, reactions: MessageReaction[]) => void;
    // Called after the connection recovers from a drop, so the caller can refetch the latest
    // messages to fill any gap missed while disconnected.
    onReconnected?: () => void;
};

/// Opens a SignalR connection scoped to one thread (joins its group), wiring up live message and
/// reaction updates. Call the returned disconnect() on unmount to leave the group and close the
/// connection.
export async function connectToThread(threadId: string, handlers: MessageboardHubHandlers): Promise<() => Promise<void>> {
    const connection: HubConnection = new HubConnectionBuilder()
        .withUrl(getHubUrl("/hubs/messageboard"), { accessTokenFactory: () => getAccessToken() ?? "" })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();

    connection.on("NewMessage", (message: Message) => handlers.onNewMessage(message));
    connection.on("ReactionsChanged", (messageId: string, reactions: MessageReaction[]) => handlers.onReactionsChanged(messageId, reactions));

    connection.onreconnected(() => {
        connection.invoke("JoinThread", threadId).catch(() => {});
        handlers.onReconnected?.();
    });

    await connection.start();
    await connection.invoke("JoinThread", threadId);

    return async () => {
        try {
            await connection.invoke("LeaveThread", threadId);
        } catch {
            // Connection may already be closed - nothing to clean up server-side in that case.
        }
        await connection.stop();
    };
}
