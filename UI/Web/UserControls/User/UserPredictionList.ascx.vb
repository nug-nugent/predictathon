Namespace Predictathon.UserControls.User
    Public Class UserPredictionList
        Inherits Predictathon.Web.UI.UserControl

#Region " Properties "
        Private _UserID As Guid
        Public Property UserID() As Guid
            Get
                Return _UserID
            End Get
            Set(ByVal value As Guid)
                _UserID = value
            End Set
        End Property
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                RebuildList()
            End If
        End Sub

        Private Sub RebuildList()
            'hide future user predictions unless this is the current user
            gvUserPredictionList.DataSource = Predictathon.MatchManager.UserMatchPredictionListGet( _
                                                Me.UserID, _
                                                Predictathon.CompetitionManager.CurrentCompetitionID, _
                                                Nothing, _
                                                If(Me.UserID = UserManager.CurrentUserID, Nothing, Date.Now), True).OrderByDescending(Function(Result) Result.MatchDateTime).ToList
            gvUserPredictionList.DataBind()
        End Sub

        Private Sub gvUserPredictionList_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvUserPredictionList.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/Match/MatchDetail.aspx?MatchID=" & gvUserPredictionList.DataKeyValue(e).ToString)
            End If
        End Sub

        Private Sub gvUserPredictionList_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvUserPredictionList.PageIndexChanging
            gvUserPredictionList.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub
    End Class
End Namespace