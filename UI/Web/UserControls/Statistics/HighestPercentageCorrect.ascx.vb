Namespace Predictathon.UserControls.Statistics
    Public Class HighestPercentageCorrect
        Inherits Predictathon.Web.UI.UserControl

        Protected Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            If Not IsPostBack Then
                RebuildList()
            End If
        End Sub

        Protected Sub RebuildList()
            gvPercentageCorrect.DataSource = HallOfFameManager.Statistics_HighestPercentageCorrectPredictionsGet()
            gvPercentageCorrect.DataBind()
        End Sub

        Private Sub gvPercentageCorrect_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvPercentageCorrect.PageIndexChanging
            gvPercentageCorrect.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub gvPercentageCorrect_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvPercentageCorrect.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/User/UserDetail.aspx?UserID=" & gvPercentageCorrect.DataKeyValue(e).ToString)
            End If
        End Sub
    End Class
End Namespace