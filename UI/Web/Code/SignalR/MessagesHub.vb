Option Strict Off
Imports Microsoft.AspNet.SignalR
Imports Microsoft.AspNet.SignalR.Hubs

<HubName("messages")>
Public Class MessagesHub
    Inherits Hub

    Public Shared Sub NewMessage(threadId As Guid)
        Dim hub = GlobalHost.ConnectionManager.GetHubContext(Of MessagesHub)()
        hub.Clients.All.newMessage(Predictathon.UserManager.CurrentUserID, threadId)
    End Sub

    Public Shared Sub ReactionsChanged(threadId As Guid, messageId As Guid)
        Dim hub = GlobalHost.ConnectionManager.GetHubContext(Of MessagesHub)()
        hub.Clients.All.reactionsChanged(Predictathon.UserManager.CurrentUserID, threadId, messageId)
    End Sub
End Class
