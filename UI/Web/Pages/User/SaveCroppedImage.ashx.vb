Imports System.Web.Script.Serialization

Namespace Predictathon.UI.Web
    Public Class SaveCroppedImage
        Implements System.Web.IHttpHandler

        Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
            context.Response.ContentType = "application/json; charset=utf-8"

            Dim js As New JavaScriptSerializer()

            Try
                If context.Request.Files.Count = 0 Then
                    context.Response.Write(js.Serialize(New With {.success = False, .error = "No files uploaded"}))
                    Return
                End If

                Dim userID As String = context.Request.Form("userID")
                If String.IsNullOrEmpty(userID) Then
                    context.Response.Write(js.Serialize(New With {.success = False, .error = "Missing userID"}))
                    Return
                End If

                Dim uploadsPath As String = context.Server.MapPath("~/Uploads/Images/")
                If Not System.IO.Directory.Exists(uploadsPath) Then
                    System.IO.Directory.CreateDirectory(uploadsPath)
                End If

                ' Expect two files: image and image_sm
                Dim fileLarge As HttpPostedFile = context.Request.Files("image")
                Dim fileSmall As HttpPostedFile = context.Request.Files("image_sm")

                If fileLarge Is Nothing OrElse fileSmall Is Nothing Then
                    context.Response.Write(js.Serialize(New With {.success = False, .error = "Both image and image_sm required"}))
                    Return
                End If

                Dim fileLargePath As String = System.IO.Path.Combine(uploadsPath, userID & ".jpg")
                Dim fileSmallPath As String = System.IO.Path.Combine(uploadsPath, userID & "_sm.jpg")

                ' Basic validation: types and size (<= 5MB each)
                Dim allowedTypes As String() = {"image/jpeg", "image/jpg"}
                If Not allowedTypes.Contains(fileLarge.ContentType.ToLower()) Or Not allowedTypes.Contains(fileSmall.ContentType.ToLower()) Then
                    context.Response.Write(js.Serialize(New With {.success = False, .error = "Invalid file type"}))
                    Return
                End If

                If fileLarge.ContentLength > 5 * 1024 * 1024 Or fileSmall.ContentLength > 5 * 1024 * 1024 Then
                    context.Response.Write(js.Serialize(New With {.success = False, .error = "File too large"}))
                    Return
                End If

                fileLarge.SaveAs(fileLargePath)
                fileSmall.SaveAs(fileSmallPath)

                ' Update user record
                Dim uid As Guid = New Guid(userID)
                Dim objUser As PredictathonModel.User = Predictathon.UserManager.Load(uid)
                If objUser IsNot Nothing Then
                    objUser.ImageUploaded = True
                    Predictathon.UserManager.Save(objUser)
                End If

                Dim imageUrl As String = VirtualPathUtility.ToAbsolute("~/Uploads/Images/" & userID & ".jpg")
                context.Response.Write(js.Serialize(New With {.success = True, .imageUrl = imageUrl}))
            Catch ex As Exception
                context.Response.Write(js.Serialize(New With {.success = False, .error = HttpUtility.HtmlEncode(ex.Message)}))
            End Try
        End Sub

        ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace
