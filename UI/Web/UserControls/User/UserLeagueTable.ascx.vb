Namespace Predictathon.UserControls.User
    Public Class UserLeagueTable
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

        Private _ShowLink As Boolean = False
        Public Property ShowLink() As Boolean
            Get
                Return _ShowLink
            End Get
            Set(ByVal value As Boolean)
                _ShowLink = value
            End Set
        End Property
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                If Not CompetitionManager.CurrentCompetition.DuplicateFixturesAllowed Then
                    RebuildList()
                    hypGridTitle.Text = "If " & Predictathon.UserManager.Load(Me.UserID).Username & "'s predictions had all come true..."
                    If Me.ShowLink Then
                        hypGridTitle.NavigateUrl = Predictathon.CommonMethods.CurrentURLRoot & "Pages/User/UserLeagueTable.aspx?UserID=" & Me.UserID.ToString
                    Else
                        hypGridTitle.NavigateUrl = String.Empty
                    End If
                Else
                    'it's a knockout competition - league table irrelevant
                    Me.Visible = False
                End If
            End If
        End Sub

        Private Sub RebuildList()
            gvCompetitionUserLeagueTable.DataSource = Predictathon.CompetitionManager.CompetitionUserLeagueTableGet(Predictathon.CompetitionManager.CurrentCompetitionID, Me.UserID).ToList
            gvCompetitionUserLeagueTable.DataBind()
        End Sub

        Private Sub gvCompetitionUserLeagueTable_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvCompetitionUserLeagueTable.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/Team/TeamDetail.aspx?TeamID=" & gvCompetitionUserLeagueTable.DataKeyValue(e).ToString)
            End If
        End Sub

        Private Sub gvCompetitionUserLeagueTable_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvCompetitionUserLeagueTable.PageIndexChanging
            gvCompetitionUserLeagueTable.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub
    End Class
End Namespace