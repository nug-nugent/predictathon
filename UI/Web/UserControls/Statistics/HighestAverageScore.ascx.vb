Namespace Predictathon.UserControls.Statistics
    Public Class HighestAverageScore
        Inherits Predictathon.Web.UI.UserControl

        Protected Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            If Not IsPostBack Then
                RebuildList()
            End If
        End Sub

        Protected Sub RebuildList()
            gvAverageScores.DataSource = HallOfFameManager.Statistics_HighestAverageScorePerPredictionsGet()
            gvAverageScores.DataBind()
        End Sub

        Private Sub gvAverageScores_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvAverageScores.PageIndexChanging
            gvAverageScores.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub gvAverageScores_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvAverageScores.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/User/UserDetail.aspx?UserID=" & gvAverageScores.DataKeyValue(e).ToString)
            End If
        End Sub
    End Class
End Namespace