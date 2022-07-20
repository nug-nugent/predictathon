Namespace Predictathon.Pages
    Partial Public Class HallOfFame
        Inherits Predictathon.Web.UI.Page

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "Hall of Fame"

            If Not IsPostBack Then
                'databind the grid
                gvHallOfFame.DataSource = Predictathon.HallOfFameManager.HallOfFameListGet()
                gvHallOfFame.DataBind()
            End If
        End Sub

        Private Sub gvHallOfFame_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvHallOfFame.RowCommand
            If e.CommandName = "EditRecord" Then
                Dim gdHallOfFameID As Guid? = gvHallOfFame.DataKeyValue(e)
            End If
        End Sub

        Protected Function HallOfFameImageURL(ByVal ImageFilename As String) As String
            Return If(String.IsNullOrEmpty(ImageFilename), "~/Images/Common/spacer.gif", "~/Images/Competitions/" & ImageFilename).ToString
        End Function

        Private Sub gvHallOfFame_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles gvHallOfFame.RowDataBound
            If e.Row.RowType = DataControlRowType.DataRow Then
                'set the NavigateURL of any records which are linked to users
                Dim objHallOfFame As PredictathonModel.HallOfFame = DirectCast(e.Row.DataItem, PredictathonModel.HallOfFame)
                If objHallOfFame.WinnerUserID.HasValue Then DirectCast(e.Row.FindControl("hypWinner"), HyperLink).NavigateUrl = "~/Pages/User/UserDetail.aspx?UserID=" & objHallOfFame.WinnerUserID.ToString
                If objHallOfFame.SecondPlaceUserID.HasValue Then DirectCast(e.Row.FindControl("hypSecondPlace"), HyperLink).NavigateUrl = "~/Pages/User/UserDetail.aspx?UserID=" & objHallOfFame.SecondPlaceUserID.ToString
                If objHallOfFame.ThirdPlaceUserID.HasValue Then DirectCast(e.Row.FindControl("hypThirdPlace"), HyperLink).NavigateUrl = "~/Pages/User/UserDetail.aspx?UserID=" & objHallOfFame.ThirdPlaceUserID.ToString
            End If
        End Sub
    End Class
End Namespace