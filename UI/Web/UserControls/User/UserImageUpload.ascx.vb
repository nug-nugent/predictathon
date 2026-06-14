Namespace Predictathon.UserControls.User
    Partial Public Class UserImageUpload
        Inherits Predictathon.Web.UI.UserControl

#Region "Properties"
        Public Property UserID() As Guid
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                Dim objUser As PredictathonModel.User = UserManager.Load(Me.UserID)
                If objUser.ImageUploaded Then
                    imgProfile.ImageUrl = "~/Uploads/Images/" & Me.UserID.ToString & ".jpg"
                Else
                    imgProfile.ImageUrl = "~/Images/Common/NoImageAvailable.gif"
                End If

                ' Render Save button but hide it via CSS so client-side script can find and show it
                btnSave.Attributes("style") = "display:none;"
                hdnUserID.Value = Me.UserID.ToString()
            End If
        End Sub

        Private Sub btnSave_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSave.Click
            lblMessage.Text = "Please use the client-side Save button to crop and upload the image."
        End Sub
    End Class
End Namespace