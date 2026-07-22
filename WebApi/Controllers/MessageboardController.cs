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

    public MessageboardController(IMessageboardService messageboardService)
    {
        _messageboardService = messageboardService;
    }

    /// <summary>
    /// Lists message threads, newest-activity first.
    /// </summary>
    [HttpGet("Threads")]
    public async Task<ActionResult<List<MessageThreadSummaryModel>?>> GetThreads(CancellationToken cancellationToken)
    {
        var result = await _messageboardService.GetThreadsAsync(CurrentUserId, cancellationToken);
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
    /// Gets a page of a thread's messages, oldest-first. With no cursor, returns the most recent
    /// page; pass the id of the oldest message currently loaded as <paramref name="beforeMessageId"/>
    /// to load older messages ("load more" / infinite scroll upward).
    /// </summary>
    [HttpGet("Thread/{threadId:guid}/Messages")]
    public async Task<ActionResult<List<MessageModel>?>> GetMessages(
        Guid threadId, [FromQuery] Guid? beforeMessageId, [FromQuery] int take, CancellationToken cancellationToken)
    {
        var pageSize = take <= 0 ? DefaultMessagePageSize : Math.Min(take, 100);
        var result = await _messageboardService.GetMessagesAsync(threadId, CurrentUserId, pageSize, beforeMessageId, cancellationToken);
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
            threadId, CurrentUserId, request.Content, request.YouTubeUrl, uploadedImage: null, request.ImageUrl, cancellationToken);

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
            threadId, CurrentUserId, request.Content, youTubeUrl: null, stream, imageUrl: null, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Adds the caller's reaction to a message.
    /// </summary>
    [HttpPost("Message/{messageId:guid}/Reactions")]
    public async Task<ActionResult<List<MessageReactionModel>?>> AddReaction(Guid messageId, AddReactionRequest request, CancellationToken cancellationToken)
    {
        var result = await _messageboardService.AddReactionAsync(messageId, CurrentUserId, request.ReactionName, request.ImageUrl, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Removes the caller's reaction from a message. The reaction name is a query parameter
    /// (rather than a route segment) since reaction names can contain spaces.
    /// </summary>
    [HttpDelete("Message/{messageId:guid}/Reactions")]
    public async Task<ActionResult<List<MessageReactionModel>?>> RemoveReaction(Guid messageId, [FromQuery] string reactionName, CancellationToken cancellationToken)
    {
        var result = await _messageboardService.RemoveReactionAsync(messageId, CurrentUserId, reactionName, cancellationToken);
        return FromResult(result);
    }
}
