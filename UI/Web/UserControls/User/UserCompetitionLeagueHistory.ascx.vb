Namespace Predictathon.UserControls.User
    Public Class UserCompetitionLeagueHistory
        Inherits Predictathon.Web.UI.UserControl

#Region "Properties"
        Private _UserID As Guid
        Public Property UserID() As Guid
            Get
                Return _UserID
            End Get
            Set(ByVal value As Guid)
                _UserID = value
            End Set
        End Property
#End Region

		Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
			If Not IsPostBack Then
				DisplayChart()
			End If
		End Sub

        Protected Sub DisplayChart()
            Dim objUser As PredictathonModel.User = Predictathon.UserManager.Load(UserID)
            lblUsername.Text = "League Position by Date - " & objUser.Username

            chtLeagueHistory.DataSource = UserCompetitionLeagueHistoryManager.UserCompetitionLeagueHistoryGet(Me.UserID, CompetitionManager.CurrentCompetitionID)
            With chtLeagueHistory.Series(0)
                .XValueMember = "Date"
                .YValueMembers = "LeaguePosition"
                .XValueType = DataVisualization.Charting.ChartValueType.Date
                .YValueType = DataVisualization.Charting.ChartValueType.Int32
            End With

            With chtLeagueHistory.ChartAreas(0)
                'always show the league's entire range, ie first to last position
                .AxisY.Minimum = 1
                .AxisY.Maximum = CDbl(Predictathon.MatchManager.LeagueTableGet(CompetitionManager.CurrentCompetitionID, Nothing, Nothing, Nothing).OrderByDescending(Function(x) x.LeaguePosition).FirstOrDefault.LeaguePosition)
            End With

            chtLeagueHistory.DataBind()
        End Sub
    End Class
End Namespace