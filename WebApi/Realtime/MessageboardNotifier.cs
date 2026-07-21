using Microsoft.AspNetCore.SignalR;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.WebApi.Hubs;

namespace Predictathon.WebApi.Realtime;

public class MessageboardNotifier : IMessageboardNotifier
{
    private readonly IHubContext<MessageboardHub> _hubContext;

    public MessageboardNotifier(IHubContext<MessageboardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public Task NotifyNewMessageAsync(Guid threadId, MessageModel message, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(MessageboardHub.GroupName(threadId))
            .SendAsync("NewMessage", message, cancellationToken);

    /// <inheritdoc />
    public Task NotifyReactionsChangedAsync(Guid threadId, Guid messageId, IReadOnlyList<MessageReactionModel> reactions, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(MessageboardHub.GroupName(threadId))
            .SendAsync("ReactionsChanged", messageId, reactions, cancellationToken);
}
