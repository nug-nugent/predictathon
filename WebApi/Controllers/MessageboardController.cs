using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Controllers.Base;
using Predictathon.WebApi.Models;

namespace Predictathon.WebApi.Controllers;

[Authorize]
public class MessageboardController : ApiControllerBase
{
    // Generous cap on the original upload, matching UserController's avatar limit.
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private const int DefaultMessagePageSize = 30;

    private readonly IMessageboardService _messageboardService;
    private readonly IReactionCatalogue _reactionCatalogue;

    public MessageboardController(IMessageboardService messageboardService, IReactionCatalogue reactionCatalogue)
    {
        _messageboardService = messageboardService;
        _reactionCatalogue = reactionCatalogue;
    }

    /// <summary>
    /// Lists Predictathon's own custom reactions, so the client's emoji picker builds its custom
    /// category from the server's manifest rather than a hardcoded copy of it. Standard Unicode
    /// emoji aren't listed here - the client already ships that dataset.
    /// </summary>
    [HttpGet("Reactions/Catalogue")]
    public ActionResult<List<CustomReactionModel>> GetReactionCatalogue()
    {
        return Ok(_reactionCatalogue.GetCustomReactions());
    }

    /// <summary>
    /// Lists a server-paged slice of message threads, newest-activity first.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of threads per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("Threads")]
    public async Task<ActionResult<PagedResult<MessageThreadSummaryModel>?>> GetThreads([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await _messageboardService.GetThreadsAsync(CurrentUserId, page < 1 ? 1 : page, pageSize < 1 ? 15 : pageSize, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Gets a single thread's detail (subject etc, not its messages).
    /// </summary>
    [HttpGet("Thread/{threadId:guid}")]
    public async Task<ActionResult<MessageThreadModel?>> GetThread(Guid threadId, CancellationToken cancellationToken)
    {
        var result = await _messageboardService.GetThreadAsync(threadId, CurrentUserId, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Gets a window of a thread's messages, oldest-first, with counts of what lies outside it
    /// either way.
    ///
    /// With no cursor, the window is anchored on the caller's first unread message so the thread
    /// opens where they left off (falling back to the newest page when they're up to date). Pass
    /// the oldest message currently loaded as <paramref name="beforeMessageId"/> to page backwards,
    /// or the newest as <paramref name="afterMessageId"/> to page forwards.
    /// </summary>
    [HttpGet("Thread/{threadId:guid}/Messages")]
    public async Task<ActionResult<MessageThreadPageModel?>> GetMessages(
        Guid threadId,
        [FromQuery] Guid? beforeMessageId,
        [FromQuery] Guid? afterMessageId,
        [FromQuery] int take,
        CancellationToken cancellationToken)
    {
        var pageSize = take <= 0 ? DefaultMessagePageSize : Math.Min(take, 100);
        var result = await _messageboardService.GetMessagesAsync(threadId, CurrentUserId, pageSize, beforeMessageId, afterMessageId, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Marks a thread as read by the caller as of now.
    /// </summary>
    [HttpPost("Thread/{threadId:guid}/MarkRead")]
    public async Task<ActionResult> MarkThreadRead(Guid threadId, CancellationToken cancellationToken)
    {
        var result = await _messageboardService.MarkThreadReadAsync(threadId, CurrentUserId, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Creates a new thread with its first message.
    /// </summary>
    [HttpPost("Thread")]
    public async Task<ActionResult<MessageThreadModel?>> CreateThread(CreateThreadRequest request, CancellationToken cancellationToken)
    {
        var result = await _messageboardService.CreateThreadAsync(CurrentUserId, request.Subject, request.FirstMessageContent, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Posts a message with plain text and/or a YouTube link and/or an externally-hosted image URL.
    /// </summary>
    [HttpPost("Thread/{threadId:guid}/Messages")]
    public async Task<ActionResult<MessageModel?>> PostMessage(Guid threadId, PostMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await _messageboardService.PostMessageAsync(
            threadId, CurrentUserId, request.Content, request.YouTubeUrl, uploadedImage: null, request.ImageUrl, request.ReplyToMessageID, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Posts a message with an uploaded image (and optional text).
    /// </summary>
    [HttpPost("Thread/{threadId:guid}/Messages/Image")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<MessageModel?>> PostMessageImage(Guid threadId, [FromForm] PostMessageImageRequest request, CancellationToken cancellationToken)
    {
        if (request.Image.Length == 0)
        {
            return BadRequestProblem(detail: "No file was uploaded.");
        }

        if (request.Image.Length > MaxUploadBytes)
        {
            return BadRequestProblem(detail: "The uploaded file is too large.");
        }

        await using var stream = request.Image.OpenReadStream();
        var result = await _messageboardService.PostMessageAsync(
            threadId, CurrentUserId, request.Content, youTubeUrl: null, stream, imageUrl: null, request.ReplyToMessageID, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Adds the caller's reaction to a message.
    /// </summary>
    [HttpPost("Message/{messageId:guid}/Reactions")]
    public async Task<ActionResult<List<MessageReactionModel>?>> AddReaction(Guid messageId, AddReactionRequest request, CancellationToken cancellationToken)
    {
        var result = await _messageboardService.AddReactionAsync(messageId, CurrentUserId, request.ReactionId, request.ReactionName, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Removes the caller's reaction from a message. The reaction identity is a query parameter
    /// rather than a route segment, since it's free-form text rather than a route-safe token.
    /// </summary>
    [HttpDelete("Message/{messageId:guid}/Reactions")]
    public async Task<ActionResult<List<MessageReactionModel>?>> RemoveReaction(Guid messageId, [FromQuery] string reactionId, CancellationToken cancellationToken)
    {
        var result = await _messageboardService.RemoveReactionAsync(messageId, CurrentUserId, reactionId, cancellationToken);
        return FromResult(result);
    }
}
