using FluentResults;
using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

public interface IMessageboardService
{
    /// <summary>
    /// Lists a server-paged slice of message threads, newest-activity first. Threads marked
    /// HiddenFromPublic are excluded unless the caller has Identity.Users.CanViewHiddenMessageThreads.
    /// Fails with a ForbiddenError if the caller can't view the messageboard at all.
    /// </summary>
    Task<Result<PagedResult<MessageThreadSummaryModel>>> GetThreadsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single thread's detail (not its messages). Fails with NotFoundError if it doesn't
    /// exist, or is hidden and the caller lacks CanViewHiddenMessageThreads.
    /// </summary>
    Task<Result<MessageThreadModel>> GetThreadAsync(Guid threadId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a page of a thread's messages, oldest-first. With no cursor, returns the most recent
    /// <paramref name="take"/> messages; with a cursor, returns up to <paramref name="take"/>
    /// messages immediately before it (for "load older messages").
    /// </summary>
    Task<Result<List<MessageModel>>> GetMessagesAsync(Guid threadId, Guid userId, int take, Guid? beforeMessageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new thread with its first message.
    /// </summary>
    Task<Result<MessageThreadModel>> CreateThreadAsync(Guid userId, string subject, string firstMessageContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a message to a thread. At most one of an uploaded image stream, an external image
    /// URL, or a YouTube link should be supplied.
    /// </summary>
    /// <param name="threadId">The thread to post to.</param>
    /// <param name="userId">The posting user.</param>
    /// <param name="content">The message text, if any.</param>
    /// <param name="youTubeUrl">A YouTube link to embed, if any.</param>
    /// <param name="uploadedImage">An uploaded image stream, if any.</param>
    /// <param name="imageUrl">An externally-hosted image URL to re-host, if any.</param>
    /// <param name="replyToMessageId">
    /// The message being replied to, or null for an ordinary post. Must be an existing message in
    /// the same thread; anything else fails validation.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<MessageModel>> PostMessageAsync(
        Guid threadId,
        Guid userId,
        string? content,
        string? youTubeUrl,
        Stream? uploadedImage,
        string? imageUrl,
        Guid? replyToMessageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the caller's reaction to a message (a no-op if they've already reacted with that exact
    /// name). Returns the message's full updated reaction list.
    /// </summary>
    Task<Result<List<MessageReactionModel>>> AddReactionAsync(Guid messageId, Guid userId, string reactionId, string reactionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the caller's reaction from a message. Returns the message's full updated reaction list.
    /// </summary>
    Task<Result<List<MessageReactionModel>>> RemoveReactionAsync(Guid messageId, Guid userId, string reactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a thread as read by the caller as of now, so it no longer shows as unread in
    /// <see cref="GetThreadsAsync"/> unless a newer message is posted after this call.
    /// </summary>
    Task<Result> MarkThreadReadAsync(Guid threadId, Guid userId, CancellationToken cancellationToken = default);
}
