using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Pushes live messageboard updates to connected clients. Implemented in WebApi (which owns the
/// SignalR hosting types) so Application stays host-agnostic.
/// </summary>
public interface IMessageboardNotifier
{
    /// <summary>
    /// Notifies connected clients that a new message has been posted to a thread.
    /// </summary>
    /// <param name="threadId">The thread the message was posted to.</param>
    /// <param name="message">The newly posted message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyNewMessageAsync(Guid threadId, MessageModel message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies connected clients that a message's reactions have changed.
    /// </summary>
    /// <param name="threadId">The thread the message belongs to.</param>
    /// <param name="messageId">The message whose reactions changed.</param>
    /// <param name="reactions">The message's full updated reaction list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyReactionsChangedAsync(Guid threadId, Guid messageId, IReadOnlyList<MessageReactionModel> reactions, CancellationToken cancellationToken = default);
}
