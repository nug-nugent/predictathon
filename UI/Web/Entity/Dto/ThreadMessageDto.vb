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
End Class
