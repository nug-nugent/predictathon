Namespace Predictathon.Pages.User
    Partial Public Class UserDetail
        Inherits Predictathon.Web.UI.Page

#Region "Properties"
        Private ReadOnly Property UserID As Guid
            Get
                Return New Guid(Request.QueryString("UserID"))
            End Get
        End Property
#End Region

        Protected Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init
            Me.Master.TitleSuffix = "User Detail"
            UserProfile1.UserID = Me.UserID
            UserPredictionList1.UserID = Me.UserID
            UserLeagueTable1.UserID = Me.UserID
            UserStatistics1.UserID = Me.UserID
			UserCompetitionLeagueHistory1.UserID = Me.UserID
        End Sub

    End Class
End Namespace