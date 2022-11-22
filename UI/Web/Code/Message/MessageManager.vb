Imports Markdig

Namespace Predictathon
	Public Class MessageManager
		Inherits Predictathon.Manager
		Public Const EntitySetName As String = "Messages"
		Private Shared markdownPipeline As MarkdownPipeline

		Public Shared Function Load(ByVal MessageID As Guid) As PredictathonModel.Message
			Using objContext As New PredictathonModel.PredictathonEntities
				Return objContext.Messages.FirstOrDefault(Function(Message) Message.MessageID = MessageID)
			End Using
		End Function

		Public Shared Function LoadThread(ByVal MessageThreadID As Guid) As List(Of ThreadMessageDto)
			Using objContext As New PredictathonModel.PredictathonEntities
				Return objContext.Messages.Where(Function(m) m.MessageThreadID = MessageThreadID) _
					.OrderBy(Function(m) m.MessageDateTime) _
					.Select(Function(m) New ThreadMessageDto With {
						.MessageId = m.MessageID,
						.MessageContent = m.MessageContent,
						.MessageDateTime = m.MessageDateTime,
						.HasLinkedImage = m.HasLinkedImage,
						.YouTubeVideoId = m.YouTubeVideoID,
						.UserPostNumber = m.UserTotalMessageboardPosts,
						.UserId = m.PostedByUserID,
						.Username = m.User.Username,
						.UserImageUploaded = m.User.ImageUploaded,
						.UserTotalPosts = m.User.TotalMessageboardPosts,
						.Reactions = m.MessageReactions.Select(Function(r) New ThreadMessageReactionDto With {
							.Name = r.ReactionName,
							.Url = r.ImageUrl,
							.Username = r.User.Username,
							.IsMe = r.UserID = UserManager.CurrentUserID
						})
					}).ToList
			End Using
		End Function

		Public Shared Sub Delete(ByVal Message As PredictathonModel.Message)
			Predictathon.Data.Delete(Message, EntitySetName)
		End Sub

		Public Shared Sub Save(ByVal Message As PredictathonModel.Message)
			'increment the user's post count
			Dim objUser As PredictathonModel.User = Predictathon.UserManager.CurrentUser
			objUser.TotalMessageboardPosts += 1
			Message.UserTotalMessageboardPosts = objUser.TotalMessageboardPosts
			Predictathon.UserManager.Save(objUser)

			Predictathon.Data.Save(Message, EntitySetName)
		End Sub

		Public Shared Function Create(ByVal MessageID As Guid, ByVal MessageThreadID As Guid) As PredictathonModel.Message
			Return New PredictathonModel.Message With {.MessageID = MessageID, .MessageThreadID = MessageThreadID, .PostedByUserID = Predictathon.UserManager.CurrentUserID, .MessageDateTime = Date.Now}
		End Function

		Public Shared Function YouTubeVideoID(ByVal YouTubeURLOrVideoID As String) As String
			If YouTubeURLOrVideoID.Length = 11 Then
				Return YouTubeURLOrVideoID
			Else
				'an example URL would be youtube.com/watch?v=Qy0MUbq7Io8&feature=g-vrec&context=G2e865b9RVAAAAAAAAAA
				' or youtu.be/Qy0MUbq7Io8
				'we only want the 11 characters after 'v=' or 'youtu.be/'
				Dim intVideoLinkIndex As Integer = YouTubeURLOrVideoID.IndexOf("?v=")
				If intVideoLinkIndex = -1 Then intVideoLinkIndex = YouTubeURLOrVideoID.IndexOf("&v=")

				If intVideoLinkIndex > 0 AndAlso YouTubeURLOrVideoID.Length >= intVideoLinkIndex + 13 Then
					Return YouTubeURLOrVideoID.Substring(intVideoLinkIndex + 3, 11)
				Else
					' try the alt url format
					intVideoLinkIndex = YouTubeURLOrVideoID.IndexOf("youtu.be/")

					If intVideoLinkIndex > 0 AndAlso YouTubeURLOrVideoID.Length >= intVideoLinkIndex + 19 Then
						Return YouTubeURLOrVideoID.Substring(intVideoLinkIndex + 9, 11)
					End If
				End If
			End If

			Return Nothing
		End Function

		Public Shared Function YouTubeVideoHTML(ByVal YouTubeVideoID As String) As String
			Return String.Format("<iframe type=""text/html"" src=""http://www.youtube.com/embed/{0}"" frameborder=""0""></iframe>", YouTubeVideoID)
		End Function

		''' <summary>
		''' Searches a string for line breaks and URLs and converts them to HTML breaks and links respectively.
		''' </summary>
		Public Shared Function FormatMessage(ByVal Message As String) As String
			If markdownPipeline Is Nothing Then
				markdownPipeline = New MarkdownPipelineBuilder().UseAdvancedExtensions().UseEmojiAndSmiley.UseSoftlineBreakAsHardlineBreak().Build()
			End If

			Return Markdown.ToHtml(Message, markdownPipeline)
		End Function
	End Class
End Namespace
