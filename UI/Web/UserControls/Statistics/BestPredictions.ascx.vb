Namespace Predictathon.UserControls.Statistics
    Public Class BestPredictions
        Inherits Predictathon.Web.UI.UserControl

        Protected Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            If Not IsPostBack Then
                RebuildList()
            End If
        End Sub

        Protected Sub RebuildList()
            gvPredictions.DataSource = Predictathon.PredictionManager.MatchPredictionAverageBiggestDifferencesGet(Predictathon.CompetitionManager.CurrentCompetitionID, Nothing, Nothing, Nothing).ToList
            gvPredictions.DataBind()
        End Sub

        Private Sub gvPredictions_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvPredictions.PageIndexChanging
            gvPredictions.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub gvPredictions_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvPredictions.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/Match/MatchDetail.aspx?MatchID=" & gvPredictions.DataKeyValue(e).ToString)
            End If
        End Sub

        Private Sub gvPredictions_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles gvPredictions.RowDataBound
            If e.Row.RowType = DataControlRowType.DataRow Then
                Dim objDataItem As PredictathonModel.MatchPredictionAverageBiggestDifferencesGet_Result = DirectCast(e.Row.DataItem, PredictathonModel.MatchPredictionAverageBiggestDifferencesGet_Result)
                DirectCast(e.Row.FindControl("lblPredictionScore"), Label).CssClass &= " " & PredictionManager.GetCSSClassForScore(objDataItem.PredictionScore)
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