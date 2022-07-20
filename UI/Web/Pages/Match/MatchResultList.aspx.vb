Namespace Predictathon.Pages.Match
    Partial Public Class MatchResultList
        Inherits Predictathon.Web.UI.Page

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "Results"

            If Not IsPostBack Then
                'databind the grid
                RebuildList()
            End If
        End Sub

        Private Sub RebuildList()
            gvMatch.DataSource = Predictathon.MatchManager.MatchResultListGet(UserManager.CurrentUserID, CompetitionManager.CurrentCompetitionID, Nothing, Nothing, Nothing)
            gvMatch.DataBind()
        End Sub

        Private Sub gvMatch_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvMatch.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/Match/MatchDetail.aspx?MatchID=" & gvMatch.DataKeyValue(e).ToString)
            End If
        End Sub

        Private Sub gvMatch_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvMatch.PageIndexChanging
            gvMatch.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub gvMatch_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles gvMatch.RowDataBound
            If e.Row.RowType = DataControlRowType.DataRow Then
                Dim objDataItem As PredictathonModel.MatchResultListGet_Result = DirectCast(e.Row.DataItem, PredictathonModel.MatchResultListGet_Result)
                Dim intScore As Integer = objDataItem.YourPredictionScore.Value
                Dim lblScore As Label = DirectCast(e.Row.FindControl("lblYourPredictionScore"), Label)
                lblScore.CssClass &= " " & PredictionManager.GetCSSClassForScore(intScore)

                DirectCast(e.Row.Cells(6).FindControl("imgComparison"), Image).ImageUrl = ComparisonImage(intScore, objDataItem.AveragePredictionScore.Value)
            End If
        End Sub

        Protected Function ComparisonImage(ByVal YourScore As Integer, ByVal AverageScore As Decimal) As String
            If YourScore > AverageScore Then
                'better
                Return CommonMethods.CurrentURLRoot & "Images/Common/UpArrow.gif"
            ElseIf YourScore < AverageScore Then
                'worse
                Return CommonMethods.CurrentURLRoot & "Images/Common/DownArrow.gif"
            Else
                'the same
                Return CommonMethods.CurrentURLRoot & "Images/Common/NoChange.gif"
            End If
        End Function
    End Class
End Namespace