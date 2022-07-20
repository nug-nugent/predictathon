Namespace Predictathon.UserControls.Statistics
    Public Class PredictableMatches
        Inherits Predictathon.Web.UI.UserControl

#Region "Properties"
        Private _ShowMostPredictable As Boolean
        Public Property ShowMostPredictable() As Boolean
            Get
                Return _ShowMostPredictable
            End Get
            Set(ByVal value As Boolean)
                _ShowMostPredictable = value
            End Set
        End Property

        Public WriteOnly Property HeaderText() As String
            Set(ByVal value As String)
                lblGridTitle.Text = value
            End Set
        End Property

        Private _MaxResults As Integer = 10
        Public Property MaxResults() As Integer
            Get
                Return _MaxResults
            End Get
            Set(ByVal value As Integer)
                _MaxResults = value
            End Set
        End Property

        Public WriteOnly Property ResultsPerPage() As Integer
            Set(ByVal value As Integer)
                gvMatchResultList.PageSize = value
            End Set
        End Property
#End Region

        Protected Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            If Not IsPostBack Then
                RebuildList()
            End If
        End Sub

        Protected Sub RebuildList()
            If Me.ShowMostPredictable Then
                gvMatchResultList.DataSource = Predictathon.MatchManager.MatchResultListGet(Predictathon.UserManager.CurrentUserID, Predictathon.CompetitionManager.CurrentCompetitionID, Nothing, Nothing, Nothing). _
                    OrderByDescending(Function(result) result.AveragePredictionScore).Take(Me.MaxResults).ToList
            Else
                gvMatchResultList.DataSource = Predictathon.MatchManager.MatchResultListGet(Predictathon.UserManager.CurrentUserID, Predictathon.CompetitionManager.CurrentCompetitionID, Nothing, Nothing, Nothing). _
                    OrderBy(Function(result) result.AveragePredictionScore).Take(Me.MaxResults).ToList
            End If
            gvMatchResultList.DataBind()
        End Sub

        Private Sub gvMatchResultList_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvMatchResultList.PageIndexChanging
            gvMatchResultList.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub gvMatchResultList_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvMatchResultList.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/Match/MatchDetail.aspx?MatchID=" & gvMatchResultList.DataKeyValue(e).ToString)
            End If
        End Sub

        Private Sub gvMatchResultList_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles gvMatchResultList.RowDataBound
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