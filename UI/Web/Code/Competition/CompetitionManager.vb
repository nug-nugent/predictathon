Namespace Predictathon
    Public Class CompetitionManager
        Inherits Predictathon.Manager
        Public Const EntitySetName As String = "Competitions"

#Region " Properties "
        Public Shared Property CurrentCompetitionID() As Guid
            Get
                If Not IsNothing(HttpContext.Current.Session("CurrentCompetitionID")) Then
                    Return New Guid(HttpContext.Current.Session("CurrentCompetitionID").ToString)
                Else
                    'no CurrentCompetitionID. Perhaps no current user, too, in which case the following method may save us...
                    If IsNothing(HttpContext.Current.Session("CurrentUserID")) Then
                        Predictathon.Security.InitialiseSessionFromCurrentIdentity()
                        If Not IsNothing(HttpContext.Current.Session("CurrentCompetitionID")) Then Return New Guid(HttpContext.Current.Session("CurrentCompetitionID").ToString)
                    End If

                    Return Nothing
                End If
            End Get
            Set(ByVal value As Guid)
                If IsNothing(value) Then
                    HttpContext.Current.Session.Remove("CurrentCompetitionID")
                    HttpContext.Current.Session.Remove("CurrentCompetitionName")
                Else
                    HttpContext.Current.Session("CurrentCompetitionID") = value
                    HttpContext.Current.Session("CurrentCompetitionName") = CurrentCompetition.CompetitionName
                End If
            End Set
        End Property

        Public Shared ReadOnly Property CurrentCompetitionName() As String
            Get
                If Not IsNothing(HttpContext.Current.Session("CurrentCompetitionName")) Then
                    Return HttpContext.Current.Session("CurrentCompetitionName").ToString
                Else
                    Return String.Empty
                End If
            End Get
        End Property

        Public Shared ReadOnly Property CurrentCompetition() As PredictathonModel.Competition
            Get
                Return Load(CurrentCompetitionID)
            End Get
        End Property
#End Region

        Public Shared Function Create(ByVal CompetitionID As Guid, ByVal CompetitionName As String) As PredictathonModel.Competition
            Return New PredictathonModel.Competition With {.CompetitionID = CompetitionID, .CompetitionName = CompetitionName, .AllowTwoPointers = True}
        End Function

        Public Shared Function Load(ByVal CompetitionID As Guid) As PredictathonModel.Competition
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Competitions.FirstOrDefault(Function(Competition) Competition.CompetitionID = CompetitionID)
            End Using
        End Function

        Public Shared Sub Delete(ByVal Competition As PredictathonModel.Competition)
            Predictathon.Data.Delete(Competition, EntitySetName)
        End Sub

        Public Shared Sub Save(ByVal Competition As PredictathonModel.Competition)
            Predictathon.Data.Save(Competition, EntitySetName)
        End Sub

        Public Shared Function CompetitionListGet() As List(Of PredictathonModel.Competition)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Competitions.OrderByDescending(Of Date)(Function(Competition) Competition.StartDate).ToList
            End Using
        End Function

        Public Shared Function CompetitionListForLoginPageGet() As List(Of PredictathonModel.Competition)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Competitions.Where(Function(Competition) Competition.RegistrationAvailableOnLoginPage).OrderByDescending(Of Date)(Function(Competition) Competition.StartDate).ToList
            End Using
        End Function

        Public Shared Function UserCompetitionRegistrationListGet(ByVal UserID As Guid) As List(Of PredictathonModel.UserCompetitionRegistrationListGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.UserCompetitionRegistrationListGet(UserID).ToList
            End Using
        End Function

        Public Shared Sub UserCompetitionLeagueHistorySet()
            'For every competition in the DB, refresh the UserCompetitionLeagueHistory table.
            'This will be used on the stats page, plus the league table (up/down/no change on last week)
            Using objContext As New PredictathonModel.PredictathonEntities
                For Each objCompetition In CompetitionManager.CompetitionListGet
                    objContext.UserCompetitionLeagueHistorySet(Date.Today, objCompetition.CompetitionID)
                Next
            End Using
        End Sub

        ''' <summary>
        ''' Returns the actual league table for a given competition
        ''' </summary>
        ''' <param name="CompetitionID"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function CompetitionRealLeagueTableGet(ByVal CompetitionID As Guid) As List(Of PredictathonModel.CompetitionRealLeagueTableGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.CompetitionRealLeagueTableGet(CompetitionID).ToList
            End Using
        End Function

        ''' <summary>
        ''' Returns the league table for a given competition as it would be if a user's predictions had all come true
        ''' </summary>
        ''' <param name="CompetitionID"></param>
        ''' <param name="UserID"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function CompetitionUserLeagueTableGet(ByVal CompetitionID As Guid, ByVal UserID As Guid) As List(Of PredictathonModel.CompetitionUserLeagueTableGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.CompetitionUserLeagueTableGet(CompetitionID, UserID).ToList
            End Using
        End Function

        ''' <summary>
        ''' Returns the list of dates that start all weeks in a competition (that contain matches).
        ''' Weeks start on a Friday so the dates returned will alwaqys be a Friday.
        ''' </summary>
        Public Shared Function GetCompetitionWeeks(competitionId As Guid) As IEnumerable(Of Date)
            Using objContext As New PredictathonModel.PredictathonEntities
                Dim knownFriday = New Date(1990, 1, 5) ' Friday Jan 5th 1990

                Return objContext.Matches.Where(Function(c) c.CompetitionID = competitionId).
                    Select(Function(m) Objects.EntityFunctions.TruncateTime(m.MatchDateTime)).
                    Distinct().
                    Select(Function(d) Objects.EntityFunctions.AddDays(d, -(Objects.EntityFunctions.DiffDays(knownFriday, d) Mod 7)).Value).
                    Distinct().
                    OrderBy(Function(d) d).
                    ToList()
            End Using
        End Function
    End Class
End Namespace
