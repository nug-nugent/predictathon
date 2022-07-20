Namespace Predictathon.UserControls.Statistics
    Public Class CompetitionWinners
        Inherits Predictathon.Web.UI.UserControl

        Protected Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            If Not IsPostBack Then
                RebuildList()
            End If
        End Sub

        Protected Sub RebuildList()
            gvWinners.DataSource = HallOfFameManager.Statistics_CompetitionWinnerListGet()
            gvWinners.DataBind()
        End Sub

        Private Sub gvWinners_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvWinners.PageIndexChanging
            gvWinners.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub gvWinners_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvWinners.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/User/UserDetail.aspx?UserID=" & gvWinners.DataKeyValue(e).ToString)
            End If
        End Sub
    End Class
End Namespace