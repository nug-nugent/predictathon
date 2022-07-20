Imports System.Data.Objects
Namespace Predictathon
    Public Class UserCompetitionLeagueHistoryManager
        Inherits Predictathon.Manager
        Public Const EntitySetName As String = "UserCompetitionLeagueHistories"

        Public Shared Function UserCompetitionLeagueHistoryGet(ByVal UserID As Guid, ByVal CompetitionID As Guid) As List(Of PredictathonModel.UserCompetitionLeagueHistoryListGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.UserCompetitionLeagueHistoryListGet(UserID, CompetitionID).ToList
            End Using
        End Function
    End Class
End Namespace
