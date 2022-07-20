Namespace Predictathon
    Public Class HallOfFameManager
        Inherits Predictathon.Manager
        Public Const EntitySetName As String = NameOf(PredictathonModel.HallOfFame)

        Public Shared Function Load(ByVal HallOfFameID As Guid) As PredictathonModel.HallOfFame
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.HallOfFame.FirstOrDefault(Function(HallOfFame) HallOfFame.HallOfFameID = HallOfFameID)
            End Using
        End Function

        Public Shared Sub Delete(ByVal HallOfFame As PredictathonModel.HallOfFame)
            Predictathon.Data.Delete(HallOfFame, EntitySetName)
        End Sub

        Public Shared Sub Save(ByVal HallOfFame As PredictathonModel.HallOfFame)
            Predictathon.Data.Save(HallOfFame, EntitySetName)
        End Sub

        Public Shared Function HallOfFameListGet() As List(Of PredictathonModel.HallOfFame)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.HallOfFameListGet.ToList
            End Using
        End Function

        Public Shared Function Statistics_CompetitionWinnerListGet() As List(Of PredictathonModel.Statistics_CompetitionWinnerListGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Statistics_CompetitionWinnerListGet.ToList
            End Using
        End Function

        Public Shared Function Statistics_HighestAllTimeScoreListGet() As List(Of PredictathonModel.Statistics_HighestAllTimeScoreListGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Statistics_HighestAllTimeScoreListGet.ToList
            End Using
        End Function

        Public Shared Function Statistics_HighestAverageScorePerPredictionsGet() As List(Of PredictathonModel.Statistics_HighestAverageScorePerPredictionsGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Statistics_HighestAverageScorePerPredictionsGet.ToList
            End Using
        End Function

        Public Shared Function Statistics_HighestPercentageCorrectPredictionsGet() As List(Of PredictathonModel.Statistics_HighestPercentageCorrectPredictionsGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Statistics_HighestPercentageCorrectPredictionsGet.ToList
            End Using
        End Function

        Public Shared Function Statistics_MostMatchesPredictedUserListGet() As List(Of PredictathonModel.Statistics_MostMatchesPredictedUserListGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Statistics_MostMatchesPredictedUserListGet.ToList
            End Using
        End Function
    End Class
End Namespace