Namespace Predictathon.UserControls.Message
    Public Class NewMessage
        Inherits Predictathon.Web.UI.UserControl

#Region " Properties "
        Public ReadOnly Property MessageThreadID() As Guid
            Get
                Return New Guid(Request.QueryString("MessageThreadID"))
            End Get
        End Property
#End Region

        Public Event NewMessageAdded()

        Private Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            rbNone.Attributes("onclick") = "ShowUploadOptions('None');"
            rbUploadImage.Attributes("onclick") = "ShowUploadOptions('Image');"
            rbYouTubeVideoLink.Attributes("onclick") = "ShowUploadOptions('Video');"

            If rbUploadImage.Checked Then
                ScriptManager.RegisterStartupScript(Me, GetType(Predictathon.UserControls.Message.NewMessage), "ShowUploadOptionsStartup", "ShowUploadOptions('Image');", True)
            ElseIf rbYouTubeVideoLink.Checked Then
                ScriptManager.RegisterStartupScript(Me, GetType(Predictathon.UserControls.Message.NewMessage), "ShowUploadOptionsStartup", "ShowUploadOptions('Video');", True)
            Else
                ScriptManager.RegisterStartupScript(Me, GetType(Predictathon.UserControls.Message.NewMessage), "ShowUploadOptionsStartup", "ShowUploadOptions('None');", True)
            End If
        End Sub

        Private Sub btnSubmit_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSubmit.Click
            If OnValidate() = True Then
                Dim blnUploadValid As Boolean = True

                'create and save a Message record
                Dim objMessage As PredictathonModel.Message = Predictathon.MessageManager.Create(Guid.NewGuid, Me.MessageThreadID)
                objMessage.MessageContent = txtMessageContent.Text

                'Link a YouTube video? Upload an image?
                If rbYouTubeVideoLink.Checked Then
                    objMessage.YouTubeVideoID = Predictathon.MessageManager.YouTubeVideoID(txtYouTubeVideoLink.Text)
                ElseIf rbUploadImage.Checked Then
                    'try to save our image as a .jpg
                    Dim strImagePath As String = Server.MapPath("~/Uploads/Images/Message/" & objMessage.MessageID.ToString & ".jpg")
                    Dim strImagePathSmall As String = Server.MapPath("~/Uploads/Images/Message/" & objMessage.MessageID.ToString & "_sm.jpg")

                    If FileUpload1.HasFile Then
                        Try
                            objMessage.HasLinkedImage = True

                            'save the image
                            ImageResizer.ImageBuilder.Current.Build(FileUpload1.FileContent, strImagePath, New ImageResizer.ResizeSettings("autorotate=true") With {.Format = "jpg", .MaxWidth = 1200, .MaxHeight = 1200})

                            'this Dispose() will likely have already happened... but just to be sure
                            FileUpload1.FileContent.Dispose()

                            '...now save a thumbnail of the image we've just saved
                            ImageResizer.ImageBuilder.Current.Build(strImagePath, strImagePathSmall, New ImageResizer.ResizeSettings() With {.Format = "jpg", .MaxWidth = 400, .MaxHeight = 400})
                        Catch ex As Exception
                            'something went wrong whilst uploading the image...
                            Validation.RenderValidation(FileUpload1, False, lblUploadFile, "")
                            blnUploadValid = False
                        End Try
                    ElseIf Not String.IsNullOrEmpty(txtUploadFromURL.Text) Then
                        Try
                            objMessage.HasLinkedImage = True

                            'save the image
                            Dim objClient As New System.Net.WebClient
                            objClient.DownloadFile(txtUploadFromURL.Text, strImagePath)
                            ImageResizer.ImageBuilder.Current.Build(strImagePath, strImagePath, New ImageResizer.ResizeSettings("autorotate=true") With {.Format = "jpg", .MaxWidth = 1200, .MaxHeight = 1200})

                            '...now save a thumbnail of the image we've just saved
                            ImageResizer.ImageBuilder.Current.Build(strImagePath, strImagePathSmall, New ImageResizer.ResizeSettings() With {.Format = "jpg", .MaxWidth = 400, .MaxHeight = 400})
                        Catch ex As Exception
                            ' Something went wrong whilst uploading the image...
                            Elmah.ErrorSignal.FromCurrentContext.Raise(ex) 'log the error

                            Validation.RenderValidation(FileUpload1, False, lblUploadFile, "")
                            blnUploadValid = False
                        End Try
                    End If
                End If

                If blnUploadValid Then
                    MessageManager.Save(objMessage)
                    MessagesHub.NewMessage(MessageThreadID)

                    txtMessageContent.Text = String.Empty
                    rbUploadImage.Checked = False
                    rbYouTubeVideoLink.Checked = False
                    rbNone.Checked = True
                    txtYouTubeVideoLink.Text = String.Empty
                    txtUploadFromURL.Text = String.Empty

                    'clear any previous validation errors
                    Validation.RenderValidation(FileUpload1, True, lblUploadFile, "")
                    Validation.RenderValidation(txtUploadFromURL, True, lblUploadFromURL, "")

                    RaiseEvent NewMessageAdded()
                ElseIf objMessage.HasLinkedImage Then
                    If Not String.IsNullOrEmpty(txtUploadFromURL.Text) Then
                        Validation.RenderValidation(txtUploadFromURL, False, lblUploadFromURL, "There was a problem uploading the image")
                    Else
                        Validation.RenderValidation(FileUpload1, False, lblUploadFile, "There was a problem uploading the image")
                    End If
                End If
            End If
        End Sub

        Private Function OnValidate() As Boolean
            Dim blnValid As Boolean = False

            'ensure that there's at least one of: text, a video, or an image
            If rbYouTubeVideoLink.Checked Then
                If String.IsNullOrEmpty(Predictathon.MessageManager.YouTubeVideoID(txtYouTubeVideoLink.Text)) Then
                    Validation.RenderValidation(txtYouTubeVideoLink, False, lblYouTubeVideoLink, "")
                    blnValid = False
                Else
                    'there is a video
                    Validation.RenderValidation(txtYouTubeVideoLink, True, lblYouTubeVideoLink, "")
                    blnValid = True
                End If
            ElseIf rbUploadImage.Checked Then
                If (Not String.IsNullOrEmpty(txtUploadFromURL.Text) OrElse Not CommonMethods.IsImage(txtUploadFromURL.Text)) _
                AndAlso (Not FileUpload1.HasFile OrElse Not CommonMethods.IsImage(FileUpload1.FileName)) Then
                    Validation.RenderValidation(FileUpload1, False, lblUploadFile, "")
                    Validation.RenderValidation(txtUploadFromURL, False, lblUploadFromURL, "")
                    blnValid = False
                Else
                    'there is an image
                    Validation.RenderValidation(FileUpload1, True, lblUploadFile, "")
                    Validation.RenderValidation(txtUploadFromURL, True, lblUploadFromURL, "")
                    blnValid = True
                End If
            End If

            If blnValid = False Then
                If String.IsNullOrEmpty(txtMessageContent.Text) Then
                    Validation.RenderValidation(txtMessageContent, False, Nothing, "")
                Else
                    blnValid = True
                    Validation.RenderValidation(txtMessageContent, True, Nothing, "")
                End If
            End If

            Return blnValid
        End Function
    End Class
End Namespace