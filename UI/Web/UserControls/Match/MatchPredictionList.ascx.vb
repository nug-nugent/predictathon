Namespace Predictathon.UserControls.Match
    Public Class MatchPredictionList
        Inherits Predictathon.Web.UI.UserControl

#Region " Properties "
        Private _MatchID As Guid
        Public Property MatchID() As Guid
            Get
                Return _MatchID
            End Get
            Set(ByVal value As Guid)
                _MatchID = value
            End Set
        End Property
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                Dim objMatch As PredictathonModel.Match = MatchManager.Load(Me.MatchID)
                If objMatch.MatchDateTime < Date.Now.AddMinutes(5) Then
                    Me.Visible = True
                    gvMatchPredictionList.DataSource = Predictathon.MatchManager.MatchPredictionListGet(Me.MatchID)
                    gvMatchPredictionList.DataBind()
                Else
                    Me.Visible = False
                End If
            End If
        End Sub

        Private Sub gvMatchPredictionList_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvMatchPredictionList.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/User/UserDetail.aspx?UserID=" & gvMatchPredictionList.DataKeyValue(e).ToString)
            End If
        End Sub

        Private blnUserHighlighted As Boolean = False
        Private Sub gvMatchPredictionList_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles gvMatchPredictionList.RowDataBound
            'highlight the current user in the grid
            If e.Row.RowType = DataControlRowType.DataRow Then
                If blnUserHighlighted = False AndAlso New Guid(gvMatchPredictionList.DataKeys(e.Row.RowIndex).Value.ToString) = UserManager.CurrentUserID Then
                    e.Row.CssClass = "GridViewRowHighlight"
                    blnUserHighlighted = True
                End If
            End If
        End Sub
    End Class
End Namespace