Namespace Predictathon.Pages
    Partial Public Class UserImageUpload
        Inherits Predictathon.Web.UI.Page

#Region "Properties"
        Protected Friend ReadOnly Property UserID As Guid
            Get
                Return New Guid(Request.QueryString("UserID"))
            End Get
        End Property
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                'display the current user's profile picture
                Dim objUser As PredictathonModel.User = UserManager.Load(Me.UserID)
                If objUser.ImageUploaded Then
                    imgProfile.ImageUrl = "~/Uploads/Images/" & Me.UserID.ToString & ".jpg"
                Else
                    imgProfile.ImageUrl = "~/Images/Common/NoImageAvailable.gif"
                End If

                btnSave.Visible = False
            End If
        End Sub

        Private Sub btnUpload_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnUpload.Click
            'TODO - priority 7 - handle massive images?
            If FileUpload1.HasFile AndAlso CommonMethods.IsImage(FileUpload1.FileName) Then
                'save the uncropped version of the uploaded image in a temporary location, then display it
                Dim strUncroppedImagePath As String = Server.MapPath("~/Uploads/Images/Temp/" & Me.UserID.ToString & "_uncropped" & System.IO.Path.GetExtension(FileUpload1.FileName))
                'persist the file extension in a hidden field, as we'll need it later
                hdnFileExtension.Value = System.IO.Path.GetExtension(FileUpload1.FileName)
                hdnImage.Value = String.Empty
                FileUpload1.SaveAs(strUncroppedImagePath)
                imgProfile.ImageUrl = "~/Uploads/Images/Temp/" & Me.UserID.ToString & "_uncropped" & System.IO.Path.GetExtension(FileUpload1.FileName) & "?i=" & Guid.NewGuid.ToString
                'attach jCrop to the image
                ScriptManager.RegisterStartupScript(Me, GetType(UserImageUpload), "ShowImageCropOption", "ShowImageCropOption();", True)
                btnSave.Visible = True
            End If
        End Sub

        Private Sub btnSave_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSave.Click
            'save the cropped version of the image.. assuming it has been cropped, that is...
            If Not String.IsNullOrEmpty(hdnImage.Value) Then
                Dim strCroppedImagePath As String = Server.MapPath("~/Uploads/Images/" & Me.UserID.ToString & ".jpg")
                Dim strCroppedImagePathSmall As String = Server.MapPath("~/Uploads/Images/" & Me.UserID.ToString & "_sm.jpg")
                'crop and save our image as a .jpg... it's this easy:
                ImageResizer.ImageBuilder.Current.Build(Server.MapPath(ImageResizer.Util.PathUtils.RemoveQueryString(hdnImage.Value)), strCroppedImagePath, New ImageResizer.ResizeSettings(hdnImage.Value) With {.Format = "jpg", .Width = 400})
                '...and a thumbnail:
                ImageResizer.ImageBuilder.Current.Build(Server.MapPath(ImageResizer.Util.PathUtils.RemoveQueryString(hdnImage.Value)), strCroppedImagePathSmall, New ImageResizer.ResizeSettings(hdnImage.Value) With {.Format = "jpg", .Width = 160})

                'delete the uncropped version we've just uploaded
                System.IO.File.Delete(Server.MapPath("~/Uploads/Images/Temp/" & Me.UserID.ToString & "_uncropped" & hdnFileExtension.Value))

                '...and use the new version as the user's profile picture
                Dim objUser As PredictathonModel.User = UserManager.Load(Me.UserID)
                objUser.ImageUploaded = True
                UserManager.Save(objUser)

                imgProfile.ImageUrl = "~/Uploads/Images/" & Me.UserID.ToString & ".jpg?t=" & Guid.NewGuid.ToString 'the querystring will avoid a cached image being displayed
            Else
                'user must crop the image. It really shouldn't come to this, though, as our JScript populates hdnImage on upload, and never clears it out.
                divProfileImage.Style("border") = "1px solid red"
            End If
        End Sub
    End Class
End Namespace