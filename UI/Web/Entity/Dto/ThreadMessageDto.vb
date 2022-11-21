Public Class ThreadMessageDto
	Private _messageId As Guid
	Public MessageContent As String
	Public MessageDateTime As Date
	Public HasLinkedImage As Boolean
	Public YouTubeVideoId As String
	Public UserPostNumber As Integer

	Public UserId As Guid
	Public Username As String
	Public UserImageUploaded As Boolean
	Public UserTotalPosts As Integer

	Public Reactions As IEnumerable(Of ThreadMessageReactionDto)

	Public Property MessageId As Guid
		Get
			Return _messageId
		End Get
		Set(value As Guid)
			_messageId = value
		End Set
	End Property

	Public Function ToJsonObject(rootPath As String) As Object
		Return New With {
			.id = _messageId,
			.authorImageUrl = rootPath + If(UserImageUploaded, "Uploads/Images/" + UserId.ToString + "_sm.jpg", "Images/Common/NoImageAvailable.gif"),
			.authorUrl = rootPath + "Pages/User/UserDetail.aspx?UserID=" + UserId.ToString,
			.authorName = Username,
			.date = MessageDateTime.ToString("s"),
			.text = MessageContent,
			.smallImageUrl = If(HasLinkedImage, rootPath + "Uploads/Images/Message/" + _messageId.ToString + "_sm.jpg", Nothing),
			.imageUrl = If(HasLinkedImage, rootPath + "Uploads/Images/Message/" + _messageId.ToString + ".jpg", Nothing),
			.youTubeVideoId = YouTubeVideoId,
			.reactions = Reactions
		}
	End Function
End Class
