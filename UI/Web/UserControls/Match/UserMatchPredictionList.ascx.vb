Namespace Predictathon.UserControls.Match
    Public Class UserMatchPredictionList
        Inherits Predictathon.Web.UI.UserControl

#Region "Properties"
        Private _DateFrom As Date
        Public Property DateFrom() As Date
            Get
                Return _DateFrom
            End Get
            Set(ByVal value As Date)
                _DateFrom = value
            End Set
        End Property

        Private _DateTo As Date
        Public Property DateTo() As Date
            Get
                Return _DateTo
            End Get
            Set(ByVal value As Date)
                _DateTo = value
            End Set
        End Property

        Public WriteOnly Property GridTitleText() As String
            Set(ByVal value As String)
                lblGridTitle.Text = value
            End Set
        End Property
#End Region

        Protected Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            If Not IsPostBack Then
                If Me.DateFrom > Date.MinValue AndAlso Me.DateTo > Date.MinValue Then
                    gvMatchPredictionList.DataSource = Predictathon.MatchManager.UserMatchPredictionListGet(Predictathon.UserManager.CurrentUserID, Predictathon.CompetitionManager.CurrentCompetitionID, Me.DateFrom, Me.DateTo)
                End If
                gvMatchPredictionList.DataBind()
            End If
        End Sub

        Private Sub gvMatchPredictionList_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvMatchPredictionList.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/Match/MatchDetail.aspx?MatchID=" & gvMatchPredictionList.DataKeyValue(e).ToString)
            End If
        End Sub

        Private Sub gvMatchPredictionList_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles gvMatchPredictionList.RowDataBound
            If e.Row.RowType = DataControlRowType.DataRow Then
                Dim objDataItem As PredictathonModel.UserMatchPredictionListGet_Result = DirectCast(e.Row.DataItem, PredictathonModel.UserMatchPredictionListGet_Result)
                'has the match kicked off and remained unprocessed?
                If objDataItem.MatchDateTime <= Date.Now AndAlso Not objDataItem.ActualHomeTeamGoals.HasValue Then
                    DirectCast(e.Row.FindControl("lblActualHomeTeamGoals"), Label).Text = "L"
                    DirectCast(e.Row.FindControl("lblActualAwayTeamGoals"), Label).Text = "L"
                End If
            End If
        End Sub
    End Class
End Namespace