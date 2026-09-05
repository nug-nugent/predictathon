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
    /// <summary>
    /// How much of a parent message's text a reply's quoted stub carries. Sized for the single
    /// line the stub renders on at a comfortable desktop width - it's truncated with an ellipsis
    /// in CSS as well, since the line's actual capacity depends on the viewport.
    /// </summary>
    private const int SnippetMaxLength = 120;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAvatarService _avatarService;
    private readonly IMessageImageService _messageImageService;
    private readonly IMessageboardNotifier _notifier;
    private readonly IReactionCatalogue _reactionCatalogue;
    private readonly ITrophyService _trophyService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessageboardService(
        IApplicationDbContext dbContext,
        IAvatarService avatarService,
        IMessageImageService messageImageService,
        IMessageboardNotifier notifier,
        IReactionCatalogue reactionCatalogue,
        ITrophyService trophyService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _avatarService = avatarService;
        _messageImageService = messageImageService;
        _notifier = notifier;
        _reactionCatalogue = reactionCatalogue;
        _trophyService = trophyService;
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

        // The parents of any replies on this page, fetched in one batch rather than through a
        // self-navigation. A parent is very often already on this page, but it doesn't have to be
        // (it can sit on an older one, or on none currently loaded), so it's looked up by id
        // regardless and the stub renders the same either way.
        var parents = await GetReplyParentsAsync(messages, cancellationToken);

        var userIds = messages
            .Select(m => m.PostedByUserID)
            .Concat(messages.SelectMany(m => m.MessageReaction.Select(r => r.UserID)))
            .Concat(parents.Values.Select(p => p.PostedByUserID));
        var users = await GetUsersByIdAsync(userIds, cancellationToken);
        var trophies = await _trophyService.GetForUsersAsync(messages.Select(m => m.PostedByUserID), cancellationToken);

        return Result.Ok(messages.Select(m => MapMessage(m, users, trophies, parents)).ToList());
    }

    /// <summary>
    /// Loads the parent messages referenced by any replies among <paramref name="messages"/>,
    /// keyed by id. Returns an empty dictionary when nothing on the page is a reply, so the common
    /// case costs no query at all.
    /// </summary>
    private async Task<Dictionary<Guid, Message>> GetReplyParentsAsync(List<Message> messages, CancellationToken cancellationToken)
    {
        var parentIds = messages
            .Where(m => m.ReplyToMessageID.HasValue)
            .Select(m => m.ReplyToMessageID!.Value)
            .Distinct()
            .ToList();

        if (parentIds.Count == 0)
        {
            return [];
        }

        var parents = await _dbContext.Message
            .AsNoTracking()
            .Where(m => parentIds.Contains(m.MessageID))
            .ToListAsync(cancellationToken);

        return parents.ToDictionary(m => m.MessageID);
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

        var postResult = await PostMessageAsync(
            thread.MessageThreadID, userId, firstMessageContent, youTubeUrl: null, uploadedImage: null, imageUrl: null, replyToMessageId: null, cancellationToken);
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
        Guid? replyToMessageId,
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

        Message? replyToMessage = null;
        if (replyToMessageId is Guid parentId)
        {
            replyToMessage = await _dbContext.Message.AsNoTracking().FirstOrDefaultAsync(m => m.MessageID == parentId, cancellationToken);
            if (replyToMessage is null)
            {
                return Result.Fail<MessageModel>(
                    new PropertyValidationError(nameof(replyToMessageId), "The message being replied to could not be found."));
            }

            // Same-thread only. Beyond being the sane reading of "reply", this is what keeps a
            // reply from quoting a message out of a thread the reader can't see: thread visibility
            // is already checked above, and a stub can never reach past it.
            if (replyToMessage.MessageThreadID != threadId)
            {
                return Result.Fail<MessageModel>(
                    new PropertyValidationError(nameof(replyToMessageId), "You can only reply to a message in the same thread."));
            }
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
            ReplyToMessageID = replyToMessageId,
        };

        await _dbContext.AddAsync(message, cancellationToken);

        viewer.TotalMessageboardPosts = newTotalPosts;
        _dbContext.Update(viewer);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // The stub is built here rather than left for the client to resolve, because this same
        // model is what gets broadcast over SignalR - every viewer of the thread has to be able to
        // draw the quote from the payload alone.
        MessageReplyReferenceModel? replyTo = null;
        if (replyToMessage is not null)
        {
            var parentAuthors = await GetUsersByIdAsync([replyToMessage.PostedByUserID], cancellationToken);
            replyTo = MapReplyReference(replyToMessage, parentAuthors);
        }

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
            PosterTrophies = [.. await _trophyService.GetForUserAsync(userId, cancellationToken)],
            ReplyTo = replyTo,
            Reactions = [],
        };

        await _notifier.NotifyNewMessageAsync(threadId, model, cancellationToken);

        return Result.Ok(model);
    }

    /// <inheritdoc />
    public async Task<Result<List<MessageReactionModel>>> AddReactionAsync(Guid messageId, Guid userId, string reactionId, string reactionName, CancellationToken cancellationToken = default)
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

        // The catalogue is the allow-list: an identity we can't resolve to an image on disk is
        // rejected outright rather than stored and left to render as a broken image for everyone.
        // Canonicalising first means the two spellings of the same emoji (see IReactionCatalogue)
        // de-duplicate against each other instead of becoming two pills.
        var canonicalId = _reactionCatalogue.Canonicalise(reactionId);
        if (canonicalId is null)
        {
            return Result.Fail<List<MessageReactionModel>>(
                new PropertyValidationError(nameof(reactionId), "That reaction isn't one this site can display."));
        }

        var alreadyReacted = await _dbContext.MessageReaction.AnyAsync(
            r => r.MessageID == messageId && r.UserID == userId && r.ReactionId == canonicalId, cancellationToken);

        if (!alreadyReacted)
        {
            await _dbContext.AddAsync(new MessageReaction
            {
                MessageReactionID = Guid.NewGuid(),
                MessageID = messageId,
                UserID = userId,
                ReactionId = canonicalId,
                ReactionName = reactionName,
                CreationDate = DateTime.UtcNow,
            }, cancellationToken);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DuplicateKeyException)
            {
                // The check above is a read-then-write, so two concurrent clicks can both pass it.
                // IX_MessageReaction_MessageID_UserID_ReactionId is what actually enforces one
                // reaction per user per identity; losing this race just means the reaction the
                // caller asked for already exists, which is the outcome they wanted anyway.
            }
        }

        var reactions = await GetReactionsAsync(messageId, cancellationToken);
        await _notifier.NotifyReactionsChangedAsync(message.MessageThreadID, messageId, reactions, cancellationToken);

        return Result.Ok(reactions);
    }

    /// <inheritdoc />
    public async Task<Result<List<MessageReactionModel>>> RemoveReactionAsync(Guid messageId, Guid userId, string reactionId, CancellationToken cancellationToken = default)
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

        // Canonicalised for the same reason as the add path: a client holding the picker's
        // spelling of an identity must still match the row stored under the canonical one.
        var canonicalId = _reactionCatalogue.Canonicalise(reactionId) ?? reactionId;

        var reaction = await _dbContext.MessageReaction.FirstOrDefaultAsync(
            r => r.MessageID == messageId && r.UserID == userId && r.ReactionId == canonicalId, cancellationToken);

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
        var users = await _dbContext.Query<ApplicationUser>().AsNoTracking().Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken);
        return users.ToDictionary(u => u.Id);
    }

    private async Task<List<MessageReactionModel>> GetReactionsAsync(Guid messageId, CancellationToken cancellationToken)
    {
        // Ordered explicitly: the client groups by identity and takes the first row's details, so
        // an unordered read would make which row wins depend on whatever order SQL happened to
        // return them in.
        var reactions = await _dbContext.MessageReaction
            .AsNoTracking()
            .Where(r => r.MessageID == messageId)
            .OrderBy(r => r.CreationDate)
            .ThenBy(r => r.MessageReactionID)
            .ToListAsync(cancellationToken);

        var users = await GetUsersByIdAsync(reactions.Select(r => r.UserID), cancellationToken);

        return reactions.Select(r => MapReaction(r, users)).ToList();
    }

    private MessageReactionModel MapReaction(MessageReaction reaction, Dictionary<Guid, ApplicationUser> users) => new()
    {
        UserID = reaction.UserID,
        Username = users.TryGetValue(reaction.UserID, out var user) ? user.UserName ?? string.Empty : string.Empty,
        ReactionId = reaction.ReactionId,
        ReactionName = reaction.ReactionName,
        ImageFile = _reactionCatalogue.ResolveImageFile(reaction.ReactionId) ?? string.Empty,
    };

    /// <summary>
    /// Builds the quoted stub for a reply's parent. Everything the client needs to draw it is
    /// flattened in here, so a stub renders identically whether or not its parent is on screen.
    /// </summary>
    private MessageReplyReferenceModel MapReplyReference(Message parent, Dictionary<Guid, ApplicationUser> users) => new()
    {
        MessageID = parent.MessageID,
        PostedByUserID = parent.PostedByUserID,
        PostedByUsername = users.TryGetValue(parent.PostedByUserID, out var author) ? author.UserName ?? string.Empty : string.Empty,
        Snippet = BuildSnippet(parent.MessageContent),
        ImageUrl = _messageImageService.GetImageUrl(parent.MessageID, parent.HasLinkedImage),
        HasYouTubeVideo = !string.IsNullOrEmpty(parent.YouTubeVideoID),
    };

    /// <summary>
    /// Truncates a parent message's content down to what the one-line stub can show, breaking at a
    /// word boundary where there is one nearby. Truncating here rather than in the client means a
    /// wall-of-text parent doesn't travel over the wire once per reply to it.
    /// </summary>
    /// <param name="content">The parent message's content, which may be null or empty.</param>
    private static string? BuildSnippet(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        // Newlines would otherwise turn a multi-line parent into a stub with a lot of blank space
        // in it: the stub is a single line, so the whole excerpt is collapsed onto one.
        var collapsed = string.Join(' ', content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (collapsed.Length <= SnippetMaxLength)
        {
            return collapsed;
        }

        var truncated = collapsed[..SnippetMaxLength];
        var lastSpace = truncated.LastIndexOf(' ');

        // Only break at a space if one falls reasonably late, otherwise a long unbroken run (a URL,
        // say) would cut the snippet down to almost nothing.
        if (lastSpace >= SnippetMaxLength / 2)
        {
            truncated = truncated[..lastSpace];
        }

        return truncated.TrimEnd() + "…";
    }

    private MessageModel MapMessage(
        Message message,
        Dictionary<Guid, ApplicationUser> users,
        IReadOnlyDictionary<Guid, List<UserTrophyModel>> trophies,
        IReadOnlyDictionary<Guid, Message> replyParents)
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
            PosterTrophies = trophies.GetValueOrDefault(message.PostedByUserID) ?? [],
            // A reply whose parent has gone missing degrades to an ordinary post rather than an
            // empty quote. The FK makes that unreachable today, but the mapping shouldn't be the
            // thing that breaks if it ever becomes reachable.
            ReplyTo = message.ReplyToMessageID is Guid parentId && replyParents.TryGetValue(parentId, out var parent)
                ? MapReplyReference(parent, users)
                : null,
            // Ordered to match GetReactionsAsync: the client groups by identity and keeps the
            // first row's details, so both paths must agree on which row that is.
            Reactions = message.MessageReaction
                .OrderBy(r => r.CreationDate)
                .ThenBy(r => r.MessageReactionID)
                .Select(r => MapReaction(r, users))
                .ToList(),
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
