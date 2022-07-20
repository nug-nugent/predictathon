Imports System.Data.Objects
Namespace Predictathon
    Public Class TeamCompetitionManager
        Inherits Predictathon.Manager
        Public Const EntitySetName As String = "TeamCompetitions"

        Public Shared Function Load(ByVal TeamCompetitionID As Guid) As PredictathonModel.TeamCompetition
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.TeamCompetitions.FirstOrDefault(Function(TeamCompetition) TeamCompetition.TeamCompetitionID = TeamCompetitionID)
            End Using
        End Function

        Public Shared Sub Delete(ByVal TeamCompetition As PredictathonModel.TeamCompetition)
            Predictathon.Data.Delete(TeamCompetition, EntitySetName)
        End Sub

        Public Shared Sub Save(ByVal TeamCompetition As PredictathonModel.TeamCompetition)
            Predictathon.Data.Save(TeamCompetition, EntitySetName)
        End Sub

        Public Shared Function Create(ByVal TeamCompetitionID As Guid, ByVal TeamID As Guid, ByVal CompetitionID As Guid) As PredictathonModel.TeamCompetition
            Return New PredictathonModel.TeamCompetition With {.TeamCompetitionID = TeamCompetitionID, _
                                                             .TeamID = TeamID, _
                                                             .CompetitionID = CompetitionID}
        End Function

        ''' <summary>
        ''' Returns a list of teams either in or not in a given competition
        ''' </summary>
        ''' <param name="CompetitionID"></param>
        ''' <param name="ReturnTeamsNotInCompetition">When true, returns only teams not in the supplied Competition.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function TeamCompetitionListGet(ByVal CompetitionID As Guid, ByVal ReturnTeamsNotInCompetition As Boolean) As List(Of PredictathonModel.TeamCompetitionListGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.TeamCompetitionListGet(CompetitionID, ReturnTeamsNotInCompetition).ToList
            End Using
        End Function
    End Class
End Namespace