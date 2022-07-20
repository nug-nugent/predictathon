Namespace Predictathon.UserControls
    Public Class LoginInformation
        Inherits Predictathon.Web.UI.UserControl

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                hypUser.Text = HttpContext.Current.User.Identity.Name
                hypUser.NavigateUrl = "~/Pages/User/UserDetail.aspx?UserID=" & Predictathon.UserManager.CurrentUserID.ToString
            End If

            lblCurrentCompetition.Text = Predictathon.CompetitionManager.CurrentCompetitionName
        End Sub

        Private Sub btnLogout_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLogout.Click
            Predictathon.Security.Logout()
        End Sub
    End Class
End Namespace