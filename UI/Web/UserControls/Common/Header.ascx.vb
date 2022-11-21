Namespace Predictathon.UserControls
    Public Class Header
        Inherits Predictathon.Web.UI.UserControl

        Private Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
            If Me.IsPostBack AndAlso Request.Form("__EVENTTARGET") = HeaderText.UniqueID Then
                ' The Header's been clicked
                Response.Redirect("~/Pages/Common/MainMenu.aspx", False)
            End If
        End Sub
    End Class
End Namespace