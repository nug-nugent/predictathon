Namespace Predictathon.Pages.Message
	Partial Public Class MessageThreadDetail
		Inherits Predictathon.Web.UI.Page

#Region " Properties "
		Public ReadOnly Property MessageThreadID() As Guid
			Get
				Return New Guid(Request.QueryString("MessageThreadID"))
			End Get
		End Property
#End Region

		Public AppPath As String
		Public CurrentUserId As String
		Public ThreadId As String
		Public ThreadTitle As String
		Public MessagesJson As String
		Public MessagesBefore As Integer
		Public MessagesAfter As Integer
		Public FirstUnreadMessageId As String

		Private Const PageSize = 10

		Private Sub MessageThreadDetail_Init(sender As Object, e As EventArgs) Handles Me.Init
			' Should the current user be here? If not, go home...
			If Not UserManager.CurrentUser.CanViewMessageboard Then Response.Redirect("~/Pages/Common/MainMenu.aspx")
		End Sub

		Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
			Me.Master.TitleSuffix = "Message Thread"

			If Not IsPostBack AndAlso Not CheckForCallBack() Then
				AppPath = ResolveUrl("~/")
				CurrentUserId = UserManager.CurrentUserID.ToString

				' load the thread details and set the first properties used by React
				Dim thread = MessageThreadManager.Load(MessageThreadID)
				ThreadId = thread.MessageThreadID.ToString
				ThreadTitle = thread.ThreadSubject.Replace("""", "\""")

				' load the thread messages and figure out where to start from
				Dim messages = MessageManager.LoadThread(MessageThreadID)
				Dim lastReadDateTime = GetLastReadDate(MessageThreadID)
				Dim firstUnreadMessage = GetFirstUnreadMessage(messages, lastReadDateTime)

				' default to the last 10 messages of the thread
				Dim messageCount = messages.Count
				Dim messagesToSkip = messageCount - PageSize

				If Not IsNothing(firstUnreadMessage) Then
					FirstUnreadMessageId = firstUnreadMessage.MessageId.ToString
					' start from 1 message behind the first unread if its earlier than the last 10
					messagesToSkip = Math.Min(messages.IndexOf(firstUnreadMessage) - 1, messagesToSkip)
				End If

				' if there's less than a page worth of messages always return them all
				If messageCount <= PageSize Then
					messagesToSkip = 0
				End If

				Dim threadPage = GetMessagesAfter(messages, messagesToSkip)

				' set the remaining properties used by React
				MessagesBefore = threadPage.MessagesBefore
				MessagesAfter = threadPage.MessagesAfter

				MessagesJson = (New Script.Serialization.JavaScriptSerializer) _
					.Serialize(threadPage.Messages.Select(Function(m) m.ToJsonObject(AppPath)))

				' update the last read time to the newest message being returned if its newer than the current value
				Dim lastMessageDate = threadPage.Messages.Last.MessageDateTime
				If lastReadDateTime.HasValue = False Or lastMessageDate > lastReadDateTime Then
					MessageThreadManager.UpdateLastReadForSession(MessageThreadID, lastMessageDate)
				End If
			End If
		End Sub

		Private Function GetLastReadDate(threadId As Guid) As Date?
			Dim dRowLastRead As DataRow = MessageThreadManager.MessageThreadsReadThisSession.Select("UniqueID = '" & threadId.ToString & "'").FirstOrDefault
			If Not IsNothing(dRowLastRead) Then
				Return CDate(dRowLastRead("DateAndTime"))
			Else
				Return CDate(Session("UserFirstViewedMessageboard"))
			End If
		End Function

		Private Function GetFirstUnreadMessage(messages As List(Of ThreadMessageDto), lastReadDateTime As Date?) As ThreadMessageDto
			If lastReadDateTime.HasValue Then
				If CDate(Session("UserFirstViewedMessageboard")) > lastReadDateTime.Value Then lastReadDateTime = CDate(Session("UserFirstViewedMessageboard"))
				Return messages.Where(Function(message) message.MessageDateTime > lastReadDateTime.Value).OrderBy(Function(message) message.MessageDateTime).FirstOrDefault
			Else
				Return Nothing
			End If
		End Function

		Public Function CheckForCallBack() As Boolean
			Dim strCallBack As String = Request.Params("CallBack")
			If String.IsNullOrEmpty(strCallBack) Then
				Return False
			ElseIf strCallBack = "GetReactions" Then
				GetReactionsFromCallback()
			ElseIf strCallBack = "AddReaction" Then
				AddPostReactionFromCallback()
			ElseIf strCallBack = "RemoveReaction" Then
				RemovePostReactionFromCallback()
			ElseIf strCallBack = "GetOlderMessages" Then
				GetMessagesBeforeFromCallback()
			ElseIf strCallBack = "GetNewerMessages" Then
				GetMessagesAfterFromCallback()
			End If
			Return True
		End Function

		Private Sub GetReactionsFromCallback()
			Try
				Dim messageId = New Guid(Request.Params("MessageId"))

				Dim reactions = MessageReactionManager.Load(messageId)
				Response.Write(MessageReactionManager.GetReactionsJson(reactions))
				Response.ContentType = "application/json"
			Catch ex As Exception
			End Try
			Response.End()
		End Sub

		Private Sub AddPostReactionFromCallback()
			Try
				Dim threadId = New Guid(Request.Params("ThreadId"))
				Dim messageId = New Guid(Request.Params("MessageId"))
				Dim name = Request.Params("Name")
				Dim url = Request.Params("ImageUrl")

				Dim reactions = MessageReactionManager.AddReaction(threadId, messageId, name, url)
				Response.Write(MessageReactionManager.GetReactionsJson(reactions))
				Response.ContentType = "application/json"
			Catch ex As Exception
			End Try
			Response.End()
		End Sub

		Private Sub RemovePostReactionFromCallback()
			Try
				Dim threadId = New Guid(Request.Params("ThreadId"))
				Dim messageId = New Guid(Request.Params("MessageId"))
				Dim name = Request.Params("Name")

				Dim reactions = MessageReactionManager.RemoveReaction(threadId, messageId, name, UserManager.CurrentUserID)
				Response.Write(MessageReactionManager.GetReactionsJson(reactions))
				Response.ContentType = "application/json"
			Catch ex As Exception
			End Try
			Response.End()
		End Sub

		Private Sub GetMessagesBeforeFromCallback()
			Try
				Dim threadId = New Guid(Request.Params("ThreadId"))
				Dim beforeMessageId = New Guid(Request.Params("BeforeMessageId"))
				Dim messages = MessageManager.LoadThread(threadId)

				Dim beforeMessageIndex = messages.FindIndex(Function(m) m.MessageId = beforeMessageId)
				Dim threadPage = GetMessagesBefore(messages, beforeMessageIndex)

				Dim websiteRoot = ResolveUrl("~/")
				Dim jsonPage = New With {
					.messagesBefore = threadPage.MessagesBefore,
					.messages = threadPage.Messages.Select(Function(m) m.ToJsonObject(websiteRoot))
				}

				Response.Write((New Script.Serialization.JavaScriptSerializer).Serialize(jsonPage))
				Response.ContentType = "application/json"
			Catch ex As Exception
			End Try
			Response.End()
		End Sub

		Private Sub GetMessagesAfterFromCallback()
			Try
				Dim threadId = New Guid(Request.Params("ThreadId"))
				Dim afterMessageId = New Guid(Request.Params("AfterMessageId"))
				Dim messages = MessageManager.LoadThread(threadId)

				Dim messagesToSkip = messages.FindIndex(Function(m) m.MessageId = afterMessageId) + 1
				Dim threadPage = GetMessagesAfter(messages, messagesToSkip)

				Dim lastReadDateTime = GetLastReadDate(threadId)
				Dim firstUnreadMessage = GetFirstUnreadMessage(messages, lastReadDateTime)

				Dim firstUnreadMessageId As Guid = Nothing
				If Not IsNothing(firstUnreadMessage) Then
					firstUnreadMessageId = firstUnreadMessage.MessageId
				End If

				Dim lastMessageDate = threadPage.Messages.Last.MessageDateTime
				If lastReadDateTime.HasValue = False Or lastMessageDate > lastReadDateTime Then
					MessageThreadManager.UpdateLastReadForSession(threadId, lastMessageDate)
				End If

				Dim websiteRoot = ResolveUrl("~/")
				Dim jsonPage = New With {
					.messagesAfter = threadPage.MessagesAfter,
					.firstUnreadMessageId = firstUnreadMessageId,
					.messages = threadPage.Messages.Select(Function(m) m.ToJsonObject(websiteRoot))
				}

				Response.Write((New Script.Serialization.JavaScriptSerializer).Serialize(jsonPage))
				Response.ContentType = "application/json"
			Catch ex As Exception
			End Try
			Response.End()
		End Sub

		Private Function GetMessagesBefore(messages As List(Of ThreadMessageDto), beforeMessageIndex As Integer) As ThreadPageDto
			Dim messageCount = messages.Count
			Dim messagesToTake = PageSize

			' if we're close to the start of the thread we may need to return less than 10 messages
			If beforeMessageIndex < messagesToTake Then
				messagesToTake = PageSize - (PageSize - beforeMessageIndex)
			End If

			Dim messagesToSkip = beforeMessageIndex - messagesToTake

			Return New ThreadPageDto With {
				.MessagesBefore = messagesToSkip,
				.Messages = messages.Skip(messagesToSkip).Take(messagesToTake)
			}
		End Function

		Private Function GetMessagesAfter(messages As List(Of ThreadMessageDto), messagesToSkip As Integer) As ThreadPageDto
			Dim messagesSlice = messages.Skip(messagesToSkip).Take(PageSize)

			Return New ThreadPageDto With {
				.MessagesBefore = messagesToSkip,
				.MessagesAfter = messages.Count - messagesToSkip - messagesSlice.Count,
				.Messages = messagesSlice
			}
		End Function

		Private Sub NewMessage1_NewMessageAdded() Handles NewMessage1.NewMessageAdded
			Predictathon.MessageThreadManager.UpdateLastReadForSession(Me.MessageThreadID)
			Response.Redirect(HttpContext.Current.Request.Url.AbsoluteUri, False)
			HttpContext.Current.ApplicationInstance.CompleteRequest()
		End Sub
	End Class
End Namespace