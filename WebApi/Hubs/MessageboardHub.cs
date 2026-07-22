using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Predictathon.WebApi.Hubs;

/// <summary>
/// Real-time push for the messageboard. Clients join a per-thread group while viewing that thread
/// so broadcasts only reach viewers of it, rather than every connected client.
/// </summary>
[Authorize]
public class MessageboardHub : Hub
{
    public Task JoinThread(Guid threadId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(threadId));

    public Task LeaveThread(Guid threadId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(threadId));

    public static string GroupName(Guid threadId) => $"thread-{threadId}";
}
