Namespace Predictathon.Pages.Message
    Partial Public Class NewMessageThread
        Inherits Predictathon.Web.UI.Page

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "New Message"
        End Sub
    End Class
End Namespace