Namespace Predictathon.Pages.Tasks
    Partial Public Class ScheduledTasks
        Inherits Predictathon.Web.UI.Page

#Region "Properties"
        Private ReadOnly Property TaskToRun As ScheduledTask
            Get
                Dim enmTaskToRun As ScheduledTask
                [Enum].TryParse(Request.QueryString("TaskToRun"), enmTaskToRun)
                Return enmTaskToRun
            End Get
        End Property
#End Region

        Protected Enum ScheduledTask
            PredictionEmailReminderSend
            UserCompetitionLeagueHistorySet
        End Enum

        Private Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init
            'don't bother with the Javascript session retention
            Me.KeepSessionAlive = False
        End Sub

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                'which task shall we run?
                Select Case TaskToRun
                    Case ScheduledTask.PredictionEmailReminderSend
                        UserManager.SendPredictionEmailReminder()
                    Case ScheduledTask.UserCompetitionLeagueHistorySet
                        CompetitionManager.UserCompetitionLeagueHistorySet()
                End Select
            End If
        End Sub
    End Class
End Namespace