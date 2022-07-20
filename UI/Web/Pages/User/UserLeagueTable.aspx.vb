Namespace Predictathon.Pages.User
    Partial Public Class UserLeagueTable
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
            UserLeagueTable1.UserID = Me.UserID
        End Sub

    End Class
End Namespace