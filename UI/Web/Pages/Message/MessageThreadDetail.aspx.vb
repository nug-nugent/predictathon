Namespace Predictathon.Pages.Message
	Partial Public Class MessageThreadDetail
		Inherits Predictathon.Web.UI.Page

#Region " Properties "
		Public ReadOnly Property MessageThreadID() As Guid
			Get
				Return New Guid(Request.QueryString("MessageThreadID"))
			End Get
		End Property

		Public ReadOnly Property PageParameter() As Int32
			Get
				Dim PageNumber As Int32 = 0
				Int32.TryParse(Request.QueryString("Page"), PageNumber)
				Return PageNumber
			End Get
		End Property
#End Region

		Private Sub MessageThreadDetail_Init(sender As Object, e As EventArgs) Handles Me.Init
			' Should the current user be here? If not, go home...
			If Not UserManager.CurrentUser.CanViewMessageboard Then Response.Redirect("~/Pages/Common/MainMenu.aspx")
		End Sub

		Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
			Me.Master.TitleSuffix = "Message Thread"

			If Not IsPostBack AndAlso Not CheckForCallBack() Then
				lblThreadSubject.Text = Predictathon.MessageThreadManager.Load(Me.MessageThreadID).ThreadSubject

				Dim PageNumber As Int32 = Me.PageParameter
				If PageNumber > 0 Then
					gvMessageList.PageIndex = PageNumber - 1
					RebuildList(False)
				Else
					'rebuild the list and scroll to the first unread message
					RebuildList(True)
				End If
				Predictathon.MessageThreadManager.UpdateLastReadForSession(Me.MessageThreadID)
			End If
		End Sub

		Public Function CheckForCallBack() As Boolean
			Dim strCallBack As String = Request.Params("CallBack")
			If String.IsNullOrEmpty(strCallBack) Then
				Return False
			ElseIf strCallBack = "AddReaction" Then
				AddPostReactionFromCallback()
			ElseIf strCallBack = "RemoveReaction" Then
				RemovePostReactionFromCallback()
			End If
			Return True
		End Function

		Private Sub AddPostReactionFromCallback()
			Try
				Dim messageId = New Guid(Request.Params("MessageId"))
				Dim name = Request.Params("Name")
				Dim url = Request.Params("ImageUrl")

				Dim reactions = MessageReactionManager.AddReaction(messageId, name, url)
				Response.Write(MessageReactionManager.GetReactionsJson(reactions))
				Response.ContentType = "application/json"
			Catch ex As Exception
			End Try
			Response.End()
		End Sub

		Private Sub RemovePostReactionFromCallback()
			Try
				Dim messageId = New Guid(Request.Params("MessageId"))
				Dim name = Request.Params("Name")

				Dim reactions = MessageReactionManager.RemoveReaction(messageId, name, UserManager.CurrentUserID)
				Response.Write(MessageReactionManager.GetReactionsJson(reactions))
				Response.ContentType = "application/json"
			Catch ex As Exception
			End Try
			Response.End()
		End Sub

		Protected Sub RebuildList(Optional ByVal MoveToFirstUnreadMessage As Boolean = False)
			Dim lstMessageThreadMessage As List(Of ThreadMessageDto) = MessageManager.LoadThread(MessageThreadID)
			Dim intScrollIndex As Integer?

			gvMessageList.DataSource = lstMessageThreadMessage

			If MoveToFirstUnreadMessage Then
				Dim dteLastReadMessage As Date?
				Dim dRowLastRead As DataRow = MessageThreadManager.MessageThreadsReadThisSession.Select("UniqueID = '" & Me.MessageThreadID.ToString & "'").FirstOrDefault

				If Not IsNothing(dRowLastRead) Then
					dteLastReadMessage = CDate(dRowLastRead("DateAndTime"))
				Else
					dteLastReadMessage = CDate(Session("UserFirstViewedMessageboard"))
				End If

				If dteLastReadMessage.HasValue Then
					If CDate(Session("UserFirstViewedMessageboard")) > dteLastReadMessage.Value Then dteLastReadMessage = CDate(Session("UserFirstViewedMessageboard"))
					Dim objMessage As ThreadMessageDto = lstMessageThreadMessage.Where(Function(message) message.MessageDateTime >= dteLastReadMessage.Value).OrderBy(Function(message) message.MessageDateTime).FirstOrDefault
					Dim intIndex As Integer
					If Not IsNothing(objMessage) Then
						intIndex = lstMessageThreadMessage.IndexOf(objMessage)
						'the index we have is actually of the last read message. We want the last _unread_ message, if there is one
						If intIndex + 1 < (lstMessageThreadMessage.Count - 1) Then intIndex += 1
					Else
						'just move to the last message
						intIndex = (lstMessageThreadMessage.Count - 1)
					End If

					'we finally know which item we're looking for... but which page is it on?
					gvMessageList.PageIndex = CInt(Math.Truncate((intIndex) / gvMessageList.PageSize))

					'...we'll need to scroll to a certain row once the grid's been databound...
					intScrollIndex = intIndex - (gvMessageList.PageSize * gvMessageList.PageIndex)
				Else
					'just move to the last page
					gvMessageList.PageIndex = CInt(Math.Truncate(lstMessageThreadMessage.Count / gvMessageList.PageSize))
				End If
			End If

			gvMessageList.DataBind()

			'...should we be scrolling to a certain record on the list?
			If intScrollIndex.HasValue Then
				gvMessageList.Rows(intScrollIndex.Value).Focus()
			End If
		End Sub

		Private Sub gvMessageList_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvMessageList.PageIndexChanging
			Response.Redirect("~/Pages/Message/MessageThreadDetail.aspx?MessageThreadID=" & MessageThreadID.ToString & "&Page=" & (e.NewPageIndex + 1))
		End Sub

		Protected Function UserImageURL(ByVal ImageUploaded As Boolean, ByVal UserID As Guid) As String
			If ImageUploaded Then
				Return "~/Uploads/Images/" & UserID.ToString & "_sm.jpg"
			Else
				Return "~/Images/Common/NoImageAvailable.gif"
			End If
		End Function

		Private Sub NewMessage1_NewMessageAdded() Handles NewMessage1.NewMessageAdded
			Predictathon.MessageThreadManager.UpdateLastReadForSession(Me.MessageThreadID)
			Response.Redirect(HttpContext.Current.Request.Url.AbsoluteUri, False)
			HttpContext.Current.ApplicationInstance.CompleteRequest()
		End Sub

		Private Sub gvMessageList_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles gvMessageList.RowDataBound
			If e.Row.RowType = DataControlRowType.DataRow Then
				Dim message = DirectCast(e.Row.DataItem, ThreadMessageDto)
				If message.HasLinkedImage Then
					Dim divLinkedImage As HtmlGenericControl = DirectCast(e.Row.FindControl("divLinkedImage"), HtmlGenericControl)
					divLinkedImage.Visible = True
					'attach JavaScript to show larger image on click
					divLinkedImage.Attributes("onclick") = String.Format("ShowPopup('{0}')", CommonMethods.CurrentURLRoot & "/Uploads/Images/Message/" & message.MessageId.ToString & ".jpg")
					DirectCast(divLinkedImage.FindControl("imgMessage"), Image).ImageUrl = "~/Uploads/Images/Message/" & message.MessageId.ToString & "_sm.jpg"
				ElseIf Not String.IsNullOrEmpty(message.YouTubeVideoId) Then
					Dim divYouTubeVideo As HtmlGenericControl = DirectCast(e.Row.FindControl("divYouTubeVideo"), HtmlGenericControl)
					divYouTubeVideo.Visible = True
					divYouTubeVideo.InnerHtml = Predictathon.MessageManager.YouTubeVideoHTML(message.YouTubeVideoId)
				End If
			End If
		End Sub
	End Class
End Namespace