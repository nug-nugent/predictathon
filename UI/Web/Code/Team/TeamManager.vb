Imports System.Data.Objects
Namespace Predictathon
    Public Class TeamManager
        Inherits Predictathon.Manager
        Public Const EntitySetName As String = "Teams"

        Public Shared Function Load(ByVal TeamID As Guid) As PredictathonModel.Team
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Teams.FirstOrDefault(Function(Team) Team.TeamID = TeamID)
            End Using
        End Function

        Public Shared Sub Delete(ByVal Team As PredictathonModel.Team)
            Predictathon.Data.Delete(Team, EntitySetName)
        End Sub

        Public Shared Sub Save(ByVal Team As PredictathonModel.Team)
            Predictathon.Data.Save(Team, EntitySetName)
        End Sub

        Public Shared Function TeamListGet(ByVal CompetitionID As Guid) As List(Of PredictathonModel.Team)
            'inner joins on TeamCompetition to ensure that only teams from the current competition are returned
            Using objContext As New PredictathonModel.PredictathonEntities
                Return (From objTeam In objContext.Teams _
                            Join objTeamCompetitions In objContext.TeamCompetitions _
                            On objTeam.TeamID Equals objTeamCompetitions.TeamID
                            Where objTeamCompetitions.CompetitionID = CompetitionID
                            Order By objTeam.TeamName
                            Select objTeam).ToList
            End Using
        End Function

        Public Shared Function RandomTeamGet(ByVal CompetitionID As Guid) As PredictathonModel.Team
            Dim objTeamList As List(Of PredictathonModel.Team) = TeamListGet(CompetitionID)
            Dim rnd As New Random
            Return objTeamList.Item(rnd.Next(0, objTeamList.Count))
        End Function

        Public Shared Function AverageScoreByTeamListGet(ByVal CompetitionID As Guid, ByVal DateFrom As Date?, ByVal DateTo As Date?) As List(Of PredictathonModel.AverageScoreByTeamListGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.AverageScoreByTeamListGet(CompetitionID, DateFrom, DateTo).ToList
            End Using
        End Function

        Public Shared Function TeamCrestImageURL(ByVal ImageName As String) As String
            Return If(String.IsNullOrEmpty(ImageName), "~/Images/Common/spacer.gif", "~/Images/TeamCrests/" & ImageName).ToString
        End Function
    End Class
End Namespace