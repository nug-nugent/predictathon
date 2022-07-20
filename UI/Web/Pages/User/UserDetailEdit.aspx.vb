Namespace Predictathon.Pages.User
    Partial Public Class UserDetailEdit
        Inherits Predictathon.Web.UI.Page

#Region "Properties"
        Protected Friend ReadOnly Property UserID As Guid
            Get
                Return New Guid(Request.QueryString("UserID"))
            End Get
        End Property
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "User Detail"

            If Not IsPostBack Then
                If Not Me.UserID = UserManager.CurrentUserID AndAlso Not UserManager.CurrentUser.UserAdministrator Then
                    'user shouldn't be here - send 'em away
                    Response.Redirect("~/Pages/User/UserDetail.aspx?UserID=" & Me.UserID.ToString, True)
                Else
                    LoadUserDetails()
                    rbUserDetail.Checked = True
                End If
            End If

            divSaveConfirmed.Visible = False
            ifImageUpload.Attributes("src") = "UserImageUpload.aspx?UserID=" & Me.UserID.ToString
            rbUserDetail.Attributes("onclick") = "ShowHideUserDetail(true);"
            rbProfilePicture.Attributes("onclick") = "ShowHideUserDetail(false);"

            ScriptManager.RegisterStartupScript(Me, GetType(UserDetailEdit), "ShowHideUserDetail", "ShowHideUserDetail(true);", True)
        End Sub

        Private Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            chkChangePassword.Attributes("onclick") = "ChangePassword(this.checked)"
            ScriptManager.RegisterStartupScript(Me, GetType(UserDetailEdit), "ChangePasswordStartup", "ChangePassword(" & chkChangePassword.Checked.ToString.ToLower & ");", True)

            Dim currentUserIsUserAdministrator As Boolean = UserManager.CurrentUser.UserAdministrator
            trCanViewMessageboard.Visible = currentUserIsUserAdministrator
            trCanViewHiddenMessageThreads.Visible = currentUserIsUserAdministrator
        End Sub

        Protected Sub LoadUserDetails()
            Dim objUser As PredictathonModel.User = UserManager.Load(UserID)
            txtUsername.Text = objUser.Username
            txtForenames.Text = objUser.Forenames
            txtSurname.Text = objUser.Surname
            txtEmailAddress.Text = objUser.EmailAddress
            txtEmailPredictionReminderDays.Text = If(IsNothing(objUser.EmailPredictionReminderDays), String.Empty, objUser.EmailPredictionReminderDays.ToString)
            txtFavouriteTeam.Text = objUser.FavouriteTeam
            txtLocation.Text = objUser.Location
            txtProfileText.Text = objUser.ProfileText
            txtCaption.Text = objUser.Caption
            chkCanViewMessageboard.Checked = objUser.CanViewMessageboard
            chkCanViewHiddenMessageThreads.Checked = objUser.CanViewHiddenMessageThreads
        End Sub

        Protected Sub SaveUserDetails()
            Dim objUser As PredictathonModel.User = UserManager.Load(UserID)
            objUser.Username = txtUsername.Text
            objUser.Forenames = txtForenames.Text
            objUser.Surname = txtSurname.Text
            objUser.EmailAddress = txtEmailAddress.Text

            If String.IsNullOrEmpty(txtEmailPredictionReminderDays.Text) Then
                objUser.EmailPredictionReminderDays = Nothing
            Else
                objUser.EmailPredictionReminderDays = CInt(txtEmailPredictionReminderDays.Text)
            End If

            objUser.FavouriteTeam = txtFavouriteTeam.Text
            objUser.Location = txtLocation.Text
            objUser.ProfileText = txtProfileText.Text
            objUser.Caption = txtCaption.Text

            If chkChangePassword.Checked Then objUser.Password = Predictathon.Security.Encryption.Encrypt(txtPassword.Text)

            If UserManager.CurrentUser.UserAdministrator Then
                objUser.CanViewMessageboard = chkCanViewMessageboard.Checked
                objUser.CanViewHiddenMessageThreads = chkCanViewHiddenMessageThreads.Checked
            End If

            UserManager.Save(objUser)

            divSaveConfirmed.Visible = True
        End Sub

        Private Sub btnSubmitUserDetails_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSubmitUserDetails.Click
            If OnValidate Then
                SaveUserDetails()
                chkChangePassword.Checked = False
            End If
        End Sub

        Private Function OnValidate() As Boolean
            Dim blnValid As Boolean = True
            Dim objUser As PredictathonModel.User = UserManager.Load(UserID)

            blnValid = blnValid And Validation.ValidateControl(txtUsername, lblUsername, Validation.ValidationType.NullOrEmpty)
            'check that the username is available, and isn't an email address
            If blnValid Then
                If txtUsername.Text <> objUser.Username Then
                    'the username has changed. Is the new one available?
                    If Predictathon.Validation.IsEmailAddress(txtUsername.Text) Then
                        blnValid = False
                        Validation.RenderValidation(txtUsername, False, lblUsername, "Please don't use an email address.")
                    ElseIf Not IsNothing(UserManager.Load(txtUsername.Text.Trim)) Then
                        blnValid = False
                        Validation.RenderValidation(txtUsername, False, lblUsername, "Sorry, this username is already taken.")
                    Else
                        Validation.RenderValidation(txtUsername, True, lblUsername, String.Empty)
                    End If
                End If
            End If

            blnValid = blnValid And Validation.ValidateControl(txtForenames, lblForenames, Validation.ValidationType.NullOrEmpty)
            blnValid = blnValid And Validation.ValidateControl(txtSurname, lblSurname, Validation.ValidationType.NullOrEmpty)
            blnValid = blnValid And Validation.ValidateControl(txtEmailAddress, lblEmailAddress, Validation.ValidationType.Email)

            If Not String.IsNullOrEmpty(txtEmailPredictionReminderDays.Text) Then
                blnValid = blnValid And Validation.ValidateControl(txtEmailPredictionReminderDays, lblEmailPredictionReminderDays, Validation.ValidationType.Numeric)
            End If

            If txtEmailAddress.Text <> objUser.EmailAddress Then
                ' The email address has changed. Check that it hasn't been used by another user
                If Validation.IsEmailAddress(txtEmailAddress.Text) Then
                    If Not IsNothing(UserManager.LoadByEmailAddress(txtEmailAddress.Text)) Then
                        Validation.RenderValidation(txtEmailAddress, False, lblEmailAddress, "Sorry, this email address has been used by another user.")
                        blnValid = False
                    Else
                        Validation.RenderValidation(txtEmailAddress, True, lblEmailAddress, String.Empty)
                    End If
                End If
            End If

            If chkChangePassword.Checked Then
                blnValid = blnValid And Validation.ValidateControl(txtPassword, lblPassword, Validation.ValidationType.NullOrEmpty)
                blnValid = blnValid And Validation.ValidateControl(txtConfirmPassword, lblConfirmPassword, Validation.ValidationType.NullOrEmpty)

                If Not String.IsNullOrEmpty(txtPassword.Text) AndAlso Not String.IsNullOrEmpty(txtConfirmPassword.Text) Then
                    'ensure the passwords match
                    If txtPassword.Text <> txtConfirmPassword.Text Then
                        blnValid = False
                        Validation.RenderValidation(txtPassword, False, lblPassword, "Passwords must match")
                        Validation.RenderValidation(txtConfirmPassword, False, lblConfirmPassword, "Passwords must match")
                    Else
                        Validation.RenderValidation(txtPassword, True, lblPassword, String.Empty)
                        Validation.RenderValidation(txtConfirmPassword, True, lblConfirmPassword, String.Empty)
                    End If
                End If
            End If

            Return blnValid
        End Function
    End Class
End Namespace