using Predictathon.Application.Models;

namespace Predictathon.Application.Interfaces;

/// <summary>
/// Pushes live messageboard updates to connected clients. Implemented in WebApi (which owns the
/// SignalR hosting types) so Application stays host-agnostic.
/// </summary>
public interface IMessageboardNotifier
{
    Task NotifyNewMessageAsync(Guid threadId, MessageModel message, CancellationToken cancellationToken = default);

    Task NotifyReactionsChangedAsync(Guid threadId, Guid messageId, IReadOnlyList<MessageReactionModel> reactions, CancellationToken cancellationToken = default);
}
