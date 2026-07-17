using FluentResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Exceptions;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class MessageboardService : IMessageboardService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAvatarService _avatarService;
    private readonly IMessageImageService _messageImageService;
    private readonly IMessageboardNotifier _notifier;

    public MessageboardService(
        IApplicationDbContext dbContext,
        IAvatarService avatarService,
        IMessageImageService messageImageService,
        IMessageboardNotifier notifier)
    {
        _dbContext = dbContext;
        _avatarService = avatarService;
        _messageImageService = messageImageService;
        _notifier = notifier;
    }

    public async Task<Result<List<MessageThreadSummaryModel>>> GetThreadsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var viewerResult = await GetViewerAsync(userId, cancellationToken);
        if (viewerResult.IsFailed)
        {
            return Result.Fail<List<MessageThreadSummaryModel>>(viewerResult.Errors);
        }

        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
            new SqlParameter("@IncludeHiddenFromPublic", SqlDbType.Bit) { Value = viewerResult.Value.CanViewHiddenMessageThreads },
        };

        var threads = await _dbContext.CallStoredProcedureAsync<MessageThreadSummaryModel>("MessageThreadListGet", parameters, cancellationToken);

        return Result.Ok(threads);
    }

    public async Task<Result<MessageThreadModel>> GetThreadAsync(Guid threadId, Guid userId, CancellationToken cancellationToken = default)
    {
        var viewerResult = await GetViewerAsync(userId, cancellationToken);
        if (viewerResult.IsFailed)
        {
            return Result.Fail<MessageThreadModel>(viewerResult.Errors);
        }

        var thread = await _dbContext.MessageThread.FirstOrDefaultAsync(t => t.MessageThreadID == threadId, cancellationToken);
        if (thread is null || (thread.HiddenFromPublic && !viewerResult.Value.CanViewHiddenMessageThreads))
        {
            return Result.Fail<MessageThreadModel>(new NotFoundError("The thread could not be found."));
        }

        return Result.Ok(new MessageThreadModel
        {
            MessageThreadID = thread.MessageThreadID,
            ThreadSubject = thread.ThreadSubject ?? "",
            HiddenFromPublic = thread.HiddenFromPublic,
        });
    }

    public async Task<Result<List<MessageModel>>> GetMessagesAsync(Guid threadId, Guid userId, int take, Guid? beforeMessageId, CancellationToken cancellationToken = default)
    {
        var threadResult = await GetThreadAsync(threadId, userId, cancellationToken);
        if (threadResult.IsFailed)
        {
            return Result.Fail<List<MessageModel>>(threadResult.Errors);
        }

        var query = _dbContext.Message.Where(m => m.MessageThreadID == threadId);

        if (beforeMessageId is Guid cursorId)
        {
            var cursorMessage = await _dbContext.Message.FirstOrDefaultAsync(m => m.MessageID == cursorId, cancellationToken);
            if (cursorMessage is not null)
            {
                query = query.Where(m => m.MessageDateTime < cursorMessage.MessageDateTime);
            }
        }

        var messages = await query
            .Include(m => m.PostedByUser)
            .Include(m => m.MessageReaction).ThenInclude(r => r.User)
            .OrderByDescending(m => m.MessageDateTime)
            .Take(take)
            .ToListAsync(cancellationToken);

        messages.Reverse();

        return Result.Ok(messages.Select(MapMessage).ToList());
    }

    public async Task<Result<MessageThreadModel>> CreateThreadAsync(Guid userId, string subject, string firstMessageContent, CancellationToken cancellationToken = default)
    {
        var viewerResult = await GetViewerAsync(userId, cancellationToken);
        if (viewerResult.IsFailed)
        {
            return Result.Fail<MessageThreadModel>(viewerResult.Errors);
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Fail<MessageThreadModel>(new PropertyValidationError(nameof(subject), "A subject is required."));
        }

        if (string.IsNullOrWhiteSpace(firstMessageContent))
        {
            return Result.Fail<MessageThreadModel>(new PropertyValidationError(nameof(firstMessageContent), "A message is required."));
        }

        var thread = new MessageThread
        {
            MessageThreadID = Guid.NewGuid(),
            ThreadSubject = subject,
            StartedByUserID = userId,
            StartedDateTime = DateTime.UtcNow,
            HiddenFromPublic = false,
        };

        await _dbContext.AddAsync(thread, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var postResult = await PostMessageAsync(thread.MessageThreadID, userId, firstMessageContent, youTubeUrl: null, uploadedImage: null, imageUrl: null, cancellationToken);
        if (postResult.IsFailed)
        {
            return Result.Fail<MessageThreadModel>(postResult.Errors);
        }

        return Result.Ok(new MessageThreadModel
        {
            MessageThreadID = thread.MessageThreadID,
            ThreadSubject = thread.ThreadSubject,
            HiddenFromPublic = false,
        });
    }

    public async Task<Result<MessageModel>> PostMessageAsync(
        Guid threadId,
        Guid userId,
        string? content,
        string? youTubeUrl,
        Stream? uploadedImage,
        string? imageUrl,
        CancellationToken cancellationToken = default)
    {
        var viewerResult = await GetViewerAsync(userId, cancellationToken);
        if (viewerResult.IsFailed)
        {
            return Result.Fail<MessageModel>(viewerResult.Errors);
        }

        var viewer = viewerResult.Value;

        var threadExists = await _dbContext.MessageThread.AnyAsync(t => t.MessageThreadID == threadId, cancellationToken);
        if (!threadExists)
        {
            return Result.Fail<MessageModel>(new NotFoundError("The thread could not be found."));
        }

        if (string.IsNullOrWhiteSpace(content) && uploadedImage is null && string.IsNullOrWhiteSpace(imageUrl) && string.IsNullOrWhiteSpace(youTubeUrl))
        {
            return Result.Fail<MessageModel>(new PropertyValidationError(nameof(content), "A message needs some content."));
        }

        var messageId = Guid.NewGuid();
        var hasLinkedImage = false;

        if (uploadedImage is not null)
        {
            var saveResult = await _messageImageService.SaveFromStreamAsync(messageId, uploadedImage, cancellationToken);
            if (saveResult.IsFailed)
            {
                return Result.Fail<MessageModel>(saveResult.Errors);
            }

            hasLinkedImage = true;
        }
        else if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            var saveResult = await _messageImageService.SaveFromUrlAsync(messageId, imageUrl, cancellationToken);
            if (saveResult.IsFailed)
            {
                return Result.Fail<MessageModel>(saveResult.Errors);
            }

            hasLinkedImage = true;
        }

        var youTubeVideoId = string.IsNullOrWhiteSpace(youTubeUrl) ? null : ParseYouTubeVideoId(youTubeUrl);
        var newTotalPosts = viewer.TotalMessageboardPosts + 1;

        var message = new Message
        {
            MessageID = messageId,
            MessageThreadID = threadId,
            PostedByUserID = userId,
            MessageDateTime = DateTime.UtcNow,
            MessageContent = content,
            YouTubeVideoID = youTubeVideoId,
            HasLinkedImage = hasLinkedImage,
            UserTotalMessageboardPosts = newTotalPosts,
        };

        await _dbContext.AddAsync(message, cancellationToken);

        viewer.TotalMessageboardPosts = newTotalPosts;
        _dbContext.Update(viewer);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var model = new MessageModel
        {
            MessageID = message.MessageID,
            MessageThreadID = threadId,
            PostedByUserID = userId,
            PostedByUsername = viewer.Username,
            PostedByAvatarUrl = _avatarService.GetAvatarUrl(userId, viewer.ImageUploaded),
            MessageDateTime = message.MessageDateTime,
            MessageContent = message.MessageContent,
            YouTubeVideoID = message.YouTubeVideoID,
            ImageUrl = _messageImageService.GetImageUrl(messageId, hasLinkedImage),
            PosterTotalMessageboardPosts = newTotalPosts,
            Reactions = [],
        };

        await _notifier.NotifyNewMessageAsync(threadId, model, cancellationToken);

        return Result.Ok(model);
    }

    public async Task<Result<List<MessageReactionModel>>> AddReactionAsync(Guid messageId, Guid userId, string reactionName, string imageUrl, CancellationToken cancellationToken = default)
    {
        var viewerResult = await GetViewerAsync(userId, cancellationToken);
        if (viewerResult.IsFailed)
        {
            return Result.Fail<List<MessageReactionModel>>(viewerResult.Errors);
        }

        var message = await _dbContext.Message.FirstOrDefaultAsync(m => m.MessageID == messageId, cancellationToken);
        if (message is null)
        {
            return Result.Fail<List<MessageReactionModel>>(new NotFoundError("The message could not be found."));
        }

        var alreadyReacted = await _dbContext.MessageReaction.AnyAsync(
            r => r.MessageID == messageId && r.UserID == userId && r.ReactionName == reactionName, cancellationToken);

        if (!alreadyReacted)
        {
            await _dbContext.AddAsync(new MessageReaction
            {
                MessageReactionID = Guid.NewGuid(),
                MessageID = messageId,
                UserID = userId,
                ReactionName = reactionName,
                ImageUrl = imageUrl,
                CreationDate = DateTime.UtcNow,
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var reactions = await GetReactionsAsync(messageId, cancellationToken);
        await _notifier.NotifyReactionsChangedAsync(message.MessageThreadID, messageId, reactions, cancellationToken);

        return Result.Ok(reactions);
    }

    public async Task<Result<List<MessageReactionModel>>> RemoveReactionAsync(Guid messageId, Guid userId, string reactionName, CancellationToken cancellationToken = default)
    {
        var viewerResult = await GetViewerAsync(userId, cancellationToken);
        if (viewerResult.IsFailed)
        {
            return Result.Fail<List<MessageReactionModel>>(viewerResult.Errors);
        }

        var message = await _dbContext.Message.FirstOrDefaultAsync(m => m.MessageID == messageId, cancellationToken);
        if (message is null)
        {
            return Result.Fail<List<MessageReactionModel>>(new NotFoundError("The message could not be found."));
        }

        var reaction = await _dbContext.MessageReaction.FirstOrDefaultAsync(
            r => r.MessageID == messageId && r.UserID == userId && r.ReactionName == reactionName, cancellationToken);

        if (reaction is not null)
        {
            _dbContext.Remove(reaction);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var reactions = await GetReactionsAsync(messageId, cancellationToken);
        await _notifier.NotifyReactionsChangedAsync(message.MessageThreadID, messageId, reactions, cancellationToken);

        return Result.Ok(reactions);
    }

    public async Task<Result> MarkThreadReadAsync(Guid threadId, Guid userId, CancellationToken cancellationToken = default)
    {
        var viewerResult = await GetViewerAsync(userId, cancellationToken);
        if (viewerResult.IsFailed)
        {
            return Result.Fail(viewerResult.Errors);
        }

        var threadExists = await _dbContext.MessageThread.AnyAsync(t => t.MessageThreadID == threadId, cancellationToken);
        if (!threadExists)
        {
            return Result.Fail(new NotFoundError("The thread could not be found."));
        }

        var readRow = await _dbContext.MessageThreadRead.FirstOrDefaultAsync(
            r => r.UserID == userId && r.MessageThreadID == threadId, cancellationToken);

        if (readRow is null)
        {
            await _dbContext.AddAsync(new MessageThreadRead
            {
                UserID = userId,
                MessageThreadID = threadId,
                LastReadDateTime = DateTime.UtcNow,
            }, cancellationToken);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DuplicateKeyException)
            {
                // A concurrent call (e.g. React StrictMode double-invoking the mount effect) already
                // inserted the row - fine, its LastReadDateTime is effectively simultaneous with ours.
            }
        }
        else
        {
            readRow.LastReadDateTime = DateTime.UtcNow;
            _dbContext.Update(readRow);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }

    private async Task<Result<User>> GetViewerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.User.FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);
        if (user is null || !user.CanViewMessageboard)
        {
            return Result.Fail<User>(new ForbiddenError("You don't have access to the messageboard."));
        }

        return Result.Ok(user);
    }

    private async Task<List<MessageReactionModel>> GetReactionsAsync(Guid messageId, CancellationToken cancellationToken)
        => await _dbContext.MessageReaction
            .Where(r => r.MessageID == messageId)
            .Select(r => new MessageReactionModel
            {
                UserID = r.UserID,
                Username = r.User.Username,
                ReactionName = r.ReactionName,
                ImageUrl = r.ImageUrl,
            })
            .ToListAsync(cancellationToken);

    private MessageModel MapMessage(Message message) => new()
    {
        MessageID = message.MessageID,
        MessageThreadID = message.MessageThreadID,
        PostedByUserID = message.PostedByUserID,
        PostedByUsername = message.PostedByUser.Username,
        PostedByAvatarUrl = _avatarService.GetAvatarUrl(message.PostedByUserID, message.PostedByUser.ImageUploaded),
        MessageDateTime = message.MessageDateTime,
        MessageContent = message.MessageContent,
        YouTubeVideoID = message.YouTubeVideoID,
        ImageUrl = _messageImageService.GetImageUrl(message.MessageID, message.HasLinkedImage),
        PosterTotalMessageboardPosts = message.UserTotalMessageboardPosts,
        Reactions = message.MessageReaction.Select(r => new MessageReactionModel
        {
            UserID = r.UserID,
            Username = r.User.Username,
            ReactionName = r.ReactionName,
            ImageUrl = r.ImageUrl,
        }).ToList(),
    };

    /// <summary>
    /// Extracts an 11-character YouTube video id from a pasted URL (watch?v=... or youtu.be/...),
    /// or returns the input unchanged if it's already just the 11-character id. Ported from the
    /// legacy MessageManager.YouTubeVideoID.
    /// </summary>
    private static string? ParseYouTubeVideoId(string urlOrVideoId)
    {
        if (urlOrVideoId.Length == 11)
        {
            return urlOrVideoId;
        }

        var index = urlOrVideoId.IndexOf("?v=", StringComparison.Ordinal);
        if (index == -1)
        {
            index = urlOrVideoId.IndexOf("&v=", StringComparison.Ordinal);
        }

        if (index > 0 && urlOrVideoId.Length >= index + 13)
        {
            return urlOrVideoId.Substring(index + 3, 11);
        }

        index = urlOrVideoId.IndexOf("youtu.be/", StringComparison.Ordinal);
        if (index > 0 && urlOrVideoId.Length >= index + 19)
        {
            return urlOrVideoId.Substring(index + 9, 11);
        }

        return null;
    }
}
