Namespace Predictathon
	Public Class MessageReactionManager
		Inherits Manager
		Public Const EntitySetName As String = "MessageReactions"

		Public Shared Function Load(messageId As Guid) As List(Of ThreadMessageReactionDto)
			Using objContext As New PredictathonModel.PredictathonEntities
				Return objContext.MessageReactions.Where(Function(r) r.MessageID = messageId) _
					.OrderBy(Function(r) r.CreationDate) _
					.Select(Function(r) New ThreadMessageReactionDto With {
						.Name = r.ReactionName,
						.Url = r.ImageUrl,
						.Username = r.User.Username,
						.IsMe = r.UserID = UserManager.CurrentUserID
					}).ToList
			End Using
		End Function

		Public Shared Function Load(messageId As Guid, name As String, userId As Guid) As PredictathonModel.MessageReaction
			Using objContext As New PredictathonModel.PredictathonEntities
				Return objContext.MessageReactions _
					.FirstOrDefault(Function(r) r.MessageID = messageId And r.ReactionName = name And r.UserID = userId)
			End Using
		End Function

		Public Shared Function AddReaction(threadId As Guid, messageId As Guid, name As String, url As String) As IEnumerable(Of ThreadMessageReactionDto)
			Dim reactions = Load(messageId)
			If reactions.Any(Function(r) r.Url = url And r.IsMe) Then
				Return reactions
			End If

			Dim newReaction = New PredictathonModel.MessageReaction With {
				.MessageReactionID = Guid.NewGuid,
				.CreationDate = Date.Now,
				.MessageID = messageId,
				.ReactionName = name,
				.ImageUrl = url,
				.UserID = UserManager.CurrentUserID
			}
			Data.Save(newReaction, EntitySetName)
			MessagesHub.ReactionsChanged(threadId, messageId)

			reactions.Add(New ThreadMessageReactionDto With {
				.Name = name,
				.Url = url,
				.Username = UserManager.CurrentUser.Username,
				.IsMe = True
			})
			Return reactions
		End Function

		Public Shared Function RemoveReaction(threadId As Guid, messageId As Guid, name As String, userId As Guid) As IEnumerable(Of ThreadMessageReactionDto)
			Dim reaction = Load(messageId, name, userId)

			If Not IsNothing(reaction) Then
				Data.Delete(reaction, EntitySetName)
				MessagesHub.ReactionsChanged(threadId, messageId)
			End If

			Return Load(messageId)
		End Function

		Public Shared Function GetReactionsJson(reactions As IEnumerable(Of ThreadMessageReactionDto)) As String
			Dim objSerializer = New Script.Serialization.JavaScriptSerializer
			Return objSerializer.Serialize(reactions)
		End Function

	End Class
End Namespace
