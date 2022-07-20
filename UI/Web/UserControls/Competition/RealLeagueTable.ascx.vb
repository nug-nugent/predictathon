Namespace Predictathon.UserControls.Competition
    Public Class RealLeagueTable
        Inherits Predictathon.Web.UI.UserControl

#Region " Properties "
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
                If Not CompetitionManager.CurrentCompetition.DuplicateFixturesAllowed Then
                    RebuildList()
                Else
                    'it's a knockout competition - league table irrelevant
                    Me.Visible = False
                End If
            End If
        End Sub

        Private Sub RebuildList()
            gvCompetitionRealLeagueTable.DataSource = Predictathon.CompetitionManager.CompetitionRealLeagueTableGet(Predictathon.CompetitionManager.CurrentCompetitionID).ToList
            gvCompetitionRealLeagueTable.DataBind()
        End Sub

        Private Sub gvCompetitionUserLeagueTable_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvCompetitionRealLeagueTable.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/Team/TeamDetail.aspx?TeamID=" & gvCompetitionRealLeagueTable.DataKeyValue(e).ToString)
            End If
        End Sub

        Private Sub gvCompetitionUserLeagueTable_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvCompetitionRealLeagueTable.PageIndexChanging
            gvCompetitionRealLeagueTable.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub
    End Class
End Namespace