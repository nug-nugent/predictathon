Namespace Predictathon
	Public Class PredictionManager
		Inherits Manager
		Public Const EntitySetName As String = "Predictions"

		Public Shared Function Load(PredictionID As Guid) As PredictathonModel.Prediction
			Using objContext As New PredictathonModel.PredictathonEntities
				Return objContext.Predictions.FirstOrDefault(Function(Prediction) Prediction.PredictionID = PredictionID)
			End Using
		End Function

		Public Shared Sub Delete(Prediction As PredictathonModel.Prediction)
			Data.Delete(Prediction, EntitySetName)
		End Sub

		Public Shared Sub Save(Prediction As PredictathonModel.Prediction)
			' Add a PredictionHistory record every time a Prediction is saved:
			Dim history = New PredictathonModel.PredictionHistory With {
				.PredictionID = Prediction.PredictionID,
				.HomeTeamGoals = Prediction.HomeTeamGoals,
				.AwayTeamGoals = Prediction.AwayTeamGoals,
				.PredictionDateTime = Now
			}

			Data.Save(history, "PredictionHistories")

			' Set the Prediction's PredictionHistoryID to the ID of the PredictionHistory we've just saved
			Prediction.PredictionHistoryID = history.PredictionHistoryID

			Data.Save(Prediction, EntitySetName)
		End Sub

		Private Shared Function Create(PredictionID As Guid, MatchID As Guid, UserID As Guid) As PredictathonModel.Prediction
			Return New PredictathonModel.Prediction With {.PredictionID = PredictionID, .MatchID = MatchID, .UserID = UserID}
		End Function

		Public Shared Function Search(MatchID As Guid, UserID As Guid) As List(Of PredictathonModel.Prediction)
			Using objContext As New PredictathonModel.PredictathonEntities
				Return objContext.Predictions.Where(Function(Prediction) Prediction.MatchID = MatchID AndAlso Prediction.UserID = UserID).ToList
			End Using
		End Function

		Public Shared Function CreateAndSave(MatchID As Guid, PredictionID As Guid?, UserID As Guid, HomeTeamGoals As Integer, AwayTeamGoals As Integer) As Guid
			Dim objPrediction As PredictathonModel.Prediction
			If PredictionID.HasValue Then
				objPrediction = Load(PredictionID.Value)
			Else
				objPrediction = Search(MatchID, UserID).FirstOrDefault()

				If objPrediction Is Nothing Then
					objPrediction = Create(Guid.NewGuid, MatchID, UserID)
				End If
			End If

			With objPrediction
				.HomeTeamGoals = HomeTeamGoals
				.AwayTeamGoals = AwayTeamGoals
			End With

			Save(objPrediction)

			Return objPrediction.PredictionID
		End Function

		Public Shared Function MatchPredictionAverageBiggestDifferencesGet(CompetitionID As Guid, DateFrom As Date?, DateTo As Date?, TeamID As Guid?) As List(Of PredictathonModel.MatchPredictionAverageBiggestDifferencesGet_Result)
			Using objContext As New PredictathonModel.PredictathonEntities
				Return objContext.MatchPredictionAverageBiggestDifferencesGet(CompetitionID, DateFrom, DateTo, TeamID).ToList
			End Using
		End Function

		Public Shared Function GetCSSClassForScore(Score As Integer, Optional ReturnFullClassString As Boolean = False) As String
			Dim strScore As String = ""
			If ReturnFullClassString Then strScore = "Score "
			Select Case Score
				Case 3
					Return strScore & "ThreePointer"
				Case 2
					Return strScore & "TwoPointer"
				Case 1
					Return strScore & "OnePointer"
				Case Else
					Return strScore & "NoPointer"
			End Select
		End Function
	End Class
End Namespace