Namespace Predictathon
    Public Class MessageRatingManager
        Inherits Predictathon.Manager
        Public Const EntitySetName As String = "MessageRatings"

        Public Shared Function Load(ByVal MessageRatingID As Guid) As PredictathonModel.MessageRating
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.MessageRatings.FirstOrDefault(Function(MessageRating) MessageRating.MessageRatingID = MessageRatingID)
            End Using
        End Function

        Public Shared Sub Delete(ByVal MessageRating As PredictathonModel.MessageRating)
            Predictathon.Data.Delete(MessageRating, EntitySetName)
        End Sub

        Public Shared Sub Save(ByVal MessageRating As PredictathonModel.MessageRating)
            Predictathon.Data.Save(MessageRating, EntitySetName)
        End Sub

        Public Shared Function Create(ByVal MessageRatingID As Guid, ByVal MessageID As Guid, ByVal RatedByUserID As Guid, ByVal Rating As Integer) As PredictathonModel.MessageRating
            Return New PredictathonModel.MessageRating With {.MessageRatingID = MessageRatingID, .MessageID = MessageID, .RatedByUserID = RatedByUserID, .Rating = Rating}
        End Function

        Public Shared Function MessageRatingListGet(ByVal MessageID As Guid) As List(Of PredictathonModel.MessageRating)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.MessageRatings.Where(Function(objMessageRating) objMessageRating.MessageID = MessageID).ToList
            End Using
        End Function
    End Class
End Namespace
