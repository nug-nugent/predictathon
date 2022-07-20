Namespace Predictathon.UserControls
    Public Class Header
        Inherits Predictathon.Web.UI.UserControl

        Private Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Me.IsPostBack AndAlso Request.Form("__EVENTTARGET") = tdMainMenu.UniqueID Then
                'divHeader's been clicked
                Response.Redirect("~/Pages/Common/MainMenu.aspx", False)
            End If
        End Sub
    End Class
End Namespace