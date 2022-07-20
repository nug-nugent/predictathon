Namespace Predictathon.UserControls.User
    Public Class PasswordReset
        Inherits Predictathon.Web.UI.UserControl

        Public Event PasswordResetRequested()

        Private Sub btnResetPassword_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnResetPassword.Click
            Dim objUser As PredictathonModel.User = UserManager.Load(txtUsernameOrEmail.Text)

            'search for the relevant user by username or email address, whichever the user supplied...
            If Validation.IsEmailAddress(txtUsernameOrEmail.Text) Then
                objUser = UserManager.LoadByEmailAddress(txtUsernameOrEmail.Text)
            Else
                objUser = UserManager.Load(txtUsernameOrEmail.Text)
            End If

            If Not IsNothing(objUser) Then
                'reset the password and email it to the user
                Predictathon.UserManager.ResetPassword(objUser, True)
                lblError.Text = "Your password has been reset. You should receive email confirmation shortly."
                lblError.Visible = True
            Else
                lblError.Text = "No matching user found"
                lblError.Visible = True
            End If

            txtUsernameOrEmail.Text = String.Empty

            RaiseEvent PasswordResetRequested()
        End Sub
    End Class
End Namespace