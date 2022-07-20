Namespace Predictathon.UserControls.Statistics
    Public Class MostPredictions
        Inherits Predictathon.Web.UI.UserControl

        Protected Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            If Not IsPostBack Then
                RebuildList()
            End If
        End Sub

        Protected Sub RebuildList()
            gvMostPredictions.DataSource = HallOfFameManager.Statistics_MostMatchesPredictedUserListGet()
            gvMostPredictions.DataBind()
        End Sub

        Private Sub gvMostPredictions_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvMostPredictions.PageIndexChanging
            gvMostPredictions.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub gvMostPredictions_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvMostPredictions.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/User/UserDetail.aspx?UserID=" & gvMostPredictions.DataKeyValue(e).ToString)
            End If
        End Sub
    End Class
End Namespace