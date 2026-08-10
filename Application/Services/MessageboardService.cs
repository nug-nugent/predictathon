using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Errors;
using Predictathon.Application.Exceptions;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class MessageboardService : IMessageboardService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAvatarService _avatarService;
    private readonly IMessageImageService _messageImageService;
    private readonly IMessageboardNotifier _notifier;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessageboardService(
        IApplicationDbContext dbContext,
        IAvatarService avatarService,
        IMessageImageService messageImageService,
        IMessageboardNotifier notifier,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _avatarService = avatarService;
        _messageImageService = messageImageService;
        _notifier = notifier;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<MessageThreadSummaryModel>>> GetThreadsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var viewerResult = await GetViewerAsync(userId, cancellationToken);
        if (viewerResult.IsFailed)
        {
            return Result.Fail<PagedResult<MessageThreadSummaryModel>>(viewerResult.Errors);
        }

        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
            new SqlParameter("@IncludeHiddenFromPublic", SqlDbType.Bit) { Value = viewerResult.Value.CanViewHiddenMessageThreads },
            new SqlParameter("@Page", SqlDbType.Int) { Value = page },
            new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize },
        };

        var rows = await _dbContext.CallStoredProcedureAsync<MessageThreadListRow>("MessageThreadListGet", parameters, cancellationToken);

        return Result.Ok(new PagedResult<MessageThreadSummaryModel>
        {
            Items = rows.Cast<MessageThreadSummaryModel>().ToList(),
            // TotalCount is a COUNT(*) OVER() column on every row (computed before OFFSET/FETCH is
            // applied), so it's the same value on each - 0 rows means nothing matched at all.
            TotalCount = rows.Count > 0 ? rows[0].TotalCount : 0,
            Page = page,
            PageSize = pageSize,
        });
    }

    /// <summary>
    /// A row from MessageThreadListGet - MessageThreadSummaryModel plus the TotalCount column the
    /// stored procedure returns alongside each row for paging. Kept out of the public
    /// MessageThreadSummaryModel/IMessageboardService surface since it's a raw-mapping detail, not
    /// something callers should see.
    /// </summary>
    private sealed class MessageThreadListRow : MessageThreadSummaryModel
    {
        public int TotalCount { get; set; }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
            .Include(m => m.MessageReaction)
            .OrderByDescending(m => m.MessageDateTime)
            .Take(take)
            .ToListAsync(cancellationToken);

        messages.Reverse();

        var userIds = messages
            .Select(m => m.PostedByUserID)
            .Concat(messages.SelectMany(m => m.MessageReaction.Select(r => r.UserID)));
        var users = await GetUsersByIdAsync(userIds, cancellationToken);

        return Result.Ok(messages.Select(m => MapMessage(m, users)).ToList());
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
            PostedByUsername = viewer.UserName ?? string.Empty,
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    private async Task<Result<ApplicationUser>> GetViewerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.CanViewMessageboard)
        {
            return Result.Fail<ApplicationUser>(new ForbiddenError("You don't have access to the messageboard."));
        }

        return Result.Ok(user);
    }

    // Message/MessageReaction have no EF navigation property into Identity.Users (it's configured
    // separately from the dbo-schema, DB-first entities - see ApplicationDbContext.Identity.cs), so
    // usernames/avatars for a batch of messages are looked up explicitly rather than via .Include.
    private async Task<Dictionary<Guid, ApplicationUser>> GetUsersByIdAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        var users = await _dbContext.Query<ApplicationUser>().Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken);
        return users.ToDictionary(u => u.Id);
    }

    private async Task<List<MessageReactionModel>> GetReactionsAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var reactions = await _dbContext.MessageReaction
            .Where(r => r.MessageID == messageId)
            .ToListAsync(cancellationToken);

        var users = await GetUsersByIdAsync(reactions.Select(r => r.UserID), cancellationToken);

        return reactions.Select(r => MapReaction(r, users)).ToList();
    }

    private static MessageReactionModel MapReaction(MessageReaction reaction, Dictionary<Guid, ApplicationUser> users) => new()
    {
        UserID = reaction.UserID,
        Username = users.TryGetValue(reaction.UserID, out var user) ? user.UserName ?? string.Empty : string.Empty,
        ReactionName = reaction.ReactionName,
        ImageUrl = reaction.ImageUrl,
    };

    private MessageModel MapMessage(Message message, Dictionary<Guid, ApplicationUser> users)
    {
        var poster = users.GetValueOrDefault(message.PostedByUserID);

        return new MessageModel
        {
            MessageID = message.MessageID,
            MessageThreadID = message.MessageThreadID,
            PostedByUserID = message.PostedByUserID,
            PostedByUsername = poster?.UserName ?? string.Empty,
            PostedByAvatarUrl = _avatarService.GetAvatarUrl(message.PostedByUserID, poster?.ImageUploaded ?? false),
            MessageDateTime = message.MessageDateTime,
            MessageContent = message.MessageContent,
            YouTubeVideoID = message.YouTubeVideoID,
            ImageUrl = _messageImageService.GetImageUrl(message.MessageID, message.HasLinkedImage),
            PosterTotalMessageboardPosts = message.UserTotalMessageboardPosts,
            Reactions = message.MessageReaction.Select(r => MapReaction(r, users)).ToList(),
        };
    }

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
