Namespace Predictathon.Pages.Message
    Partial Public Class Messageboard
        Inherits Predictathon.Web.UI.Page

        Private Sub Messageboard_Init(sender As Object, e As EventArgs) Handles Me.Init
            ' Should the current user be here? If not, go home...
            If Not UserManager.CurrentUser.CanViewMessageboard Then Response.Redirect("~/Pages/Common/MainMenu.aspx")
        End Sub

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "Messageboard"
        End Sub
    End Class
End Namespace