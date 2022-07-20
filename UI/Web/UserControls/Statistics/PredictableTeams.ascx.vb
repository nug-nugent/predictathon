Namespace Predictathon.UserControls.Statistics
    Public Class PredictableTeams
        Inherits Predictathon.Web.UI.UserControl

#Region "Properties"
        Private _DateFrom As Date?
        Public Property DateFrom() As Date?
            Get
                Return _DateFrom
            End Get
            Set(ByVal value As Date?)
                _DateFrom = value
            End Set
        End Property

        Private _DateTo As Date?
        Public Property DateTo() As Date?
            Get
                Return _DateTo
            End Get
            Set(ByVal value As Date?)
                _DateTo = value
            End Set
        End Property

        Public WriteOnly Property GridTitleText() As String
            Set(ByVal value As String)
                lblGridTitle.Text = value
            End Set
        End Property
#End Region

        Protected Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            If Not IsPostBack Then
                gvPredictableTeams.DataSource = Predictathon.TeamManager.AverageScoreByTeamListGet(Predictathon.CompetitionManager.CurrentCompetitionID, Me.DateFrom, Me.DateTo)
                gvPredictableTeams.DataBind()
            End If
        End Sub

        Private Sub gvMatchPredictionList_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvPredictableTeams.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/Team/TeamDetail.aspx?TeamID=" & gvPredictableTeams.DataKeyValue(e).ToString)
            End If
        End Sub
    End Class
End Namespace