Namespace Predictathon.UserControls.Statistics
    Public Class HighestAllTimeScore
        Inherits Predictathon.Web.UI.UserControl

        Protected Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            If Not IsPostBack Then
                RebuildList()
            End If
        End Sub

        Protected Sub RebuildList()
            gvAllTimeScores.DataSource = HallOfFameManager.Statistics_HighestAllTimeScoreListGet()
            gvAllTimeScores.DataBind()
        End Sub

        Private Sub gvAllTimeScores_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvAllTimeScores.PageIndexChanging
            gvAllTimeScores.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub gvAllTimeScores_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvAllTimeScores.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/User/UserDetail.aspx?UserID=" & gvAllTimeScores.DataKeyValue(e).ToString)
            End If
        End Sub
    End Class
End Namespace