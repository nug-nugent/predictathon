import {
    getJsonAuthenticated, postJsonAuthenticated, postFormAuthenticated, deleteJsonAuthenticated,
} from "./api";
import { type PagedResult } from "./users-admin-service";
import type { UserTrophy } from "./trophy-service";

// Matches Application/Models/MessageThreadSummaryModel.cs.
export type MessageThreadSummary = {
    messageThreadID: string;
    threadSubject: string;
    startedByUsername: string;
    startedDateTime: string;
    hiddenFromPublic: boolean;
    messageCount: number;
    lastMessageDateTime: string;
    lastMessageByUsername: string;
    unread: boolean;
};

// Matches Application/Models/MessageThreadModel.cs.
export type MessageThread = {
    messageThreadID: string;
    threadSubject: string;
    hiddenFromPublic: boolean;
};

// Matches Application/Models/MessageReactionModel.cs. `reactionId` is the identity (reactions
// group and toggle on it); `imageFile` is a bare filename under the API's /reactions mount, which
// the client turns into a URL via getReactionImageUrl - no URL is ever stored server-side.
export type MessageReaction = {
    userID: string;
    username: string;
    reactionId: string;
    reactionName: string;
    imageFile: string;
};

// Matches Application/Models/CustomReactionModel.cs.
export type CustomReaction = {
    id: string;
    name: string;
    imageFile: string;
};

// Matches Application/Models/MessageModel.cs.
export type Message = {
    messageID: string;
    messageThreadID: string;
    postedByUserID: string;
    postedByUsername: string;
    postedByAvatarUrl: string | null;
    messageDateTime: string;
    messageContent: string | null;
    youTubeVideoID: string | null;
    imageUrl: string | null;
    posterTotalMessageboardPosts: number;
    posterTrophies: UserTrophy[];
    reactions: MessageReaction[];
};

export async function getThreads(page: number, pageSize: number): Promise<PagedResult<MessageThreadSummary>> {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return getJsonAuthenticated<PagedResult<MessageThreadSummary>>(`/Messageboard/Threads?${params.toString()}`);
}

export async function getThread(threadId: string): Promise<MessageThread> {
    return getJsonAuthenticated<MessageThread>(`/Messageboard/Thread/${threadId}`);
}

/// Gets a page of a thread's messages, oldest-first. Omit beforeMessageId for the latest page;
/// pass the id of the oldest currently-loaded message to load older messages.
export async function getMessages(threadId: string, beforeMessageId?: string, take = 30): Promise<Message[]> {
    const params = new URLSearchParams({ take: String(take) });
    if (beforeMessageId) {
        params.set("beforeMessageId", beforeMessageId);
    }

    return getJsonAuthenticated<Message[]>(`/Messageboard/Thread/${threadId}/Messages?${params}`);
}

export async function createThread(subject: string, firstMessageContent: string): Promise<MessageThread> {
    return postJsonAuthenticated<MessageThread>("/Messageboard/Thread", { subject, firstMessageContent });
}

export async function postMessage(threadId: string, content: string | null, youTubeUrl?: string, imageUrl?: string): Promise<Message> {
    return postJsonAuthenticated<Message>(`/Messageboard/Thread/${threadId}/Messages`, { content, youTubeUrl, imageUrl });
}

export async function postMessageWithImage(threadId: string, image: File, content: string | null): Promise<Message> {
    const form = new FormData();
    form.append("image", image);
    if (content) {
        form.append("content", content);
    }

    return postFormAuthenticated<Message>(`/Messageboard/Thread/${threadId}/Messages/Image`, form);
}

export async function addReaction(messageId: string, reactionId: string, reactionName: string): Promise<MessageReaction[]> {
    return postJsonAuthenticated<MessageReaction[]>(`/Messageboard/Message/${messageId}/Reactions`, { reactionId, reactionName });
}

export async function removeReaction(messageId: string, reactionId: string): Promise<MessageReaction[]> {
    const params = new URLSearchParams({ reactionId });
    return deleteJsonAuthenticated<MessageReaction[]>(`/Messageboard/Message/${messageId}/Reactions?${params}`);
}

/// Predictathon's own custom reactions, as listed by the server's manifest. Standard Unicode
/// emoji aren't included - the client already ships that dataset.
export async function getReactionCatalogue(): Promise<CustomReaction[]> {
    return getJsonAuthenticated<CustomReaction[]>("/Messageboard/Reactions/Catalogue");
}

export async function markThreadRead(threadId: string): Promise<void> {
    return postJsonAuthenticated<void>(`/Messageboard/Thread/${threadId}/MarkRead`, {});
}
