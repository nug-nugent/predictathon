Namespace Predictathon.Pages
    Partial Public Class Login
        Inherits System.Web.UI.Page

        Private Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                ' If we already have a current user (via a persistent cookie, most likely), redirect straight to the main menu
                If Predictathon.Security.UserIsAuthenticated Then
                    Response.Redirect("~/Pages/Common/MainMenu.aspx")
                Else
                    ' No current user
                    RebuildList()

                    If Not String.IsNullOrEmpty(hdnCompetitionID.Value) Then
                        Rules1.CompetitionID = New Guid(hdnCompetitionID.Value)
                    End If
                End If
            End If

            txtUsername.Focus()
        End Sub

        Private Sub RebuildList()
            rptCompetitions.DataSource = CompetitionManager.CompetitionListForLoginPageGet()
            rptCompetitions.DataBind()

            If rptCompetitions.Items.Count = 0 Then
                lblNotOpenForRegistration.Visible = True
                lblOpenForRegistration.Visible = False
            Else
                lblOpenForRegistration.Visible = True
                lblNotOpenForRegistration.Visible = False
            End If
        End Sub

        Private Function ValidateInput() As Boolean
            Dim blnValid As Boolean = True

            ' Username
            If String.IsNullOrWhiteSpace(txtUsername.Text) Then
                txtUsername.CssClass = "InputError"
                blnValid = False
            Else
                txtUsername.CssClass = ""
            End If

            ' Password
            If String.IsNullOrWhiteSpace(txtPassword.Text) Then
                txtPassword.CssClass = "InputError"
                blnValid = False
            Else
                txtPassword.CssClass = ""
            End If

            Return blnValid
        End Function

        Private Sub AttemptLogin()
            If Predictathon.Security.Authenticate(txtUsername.Text, txtPassword.Text, chkRememberMe.Checked) Then
                ' Login successful
                Response.Redirect("~/Pages/Common/MainMenu.aspx")
            Else
                lblError.Visible = True
            End If
        End Sub

        Private Sub btnLogIn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLogIn.Click
            If ValidateInput() Then AttemptLogin()
        End Sub

        Protected Function ImageURL(ByVal ImageName As String) As String
            Return If(String.IsNullOrEmpty(ImageName), "~/Images/Common/spacer.gif", "~/Images/Competitions/" & ImageName).ToString
        End Function

        Private Sub PasswordReset1_PasswordResetRequested() Handles PasswordReset1.PasswordResetRequested
            'ensure the password reset option is still shown on postback
            ScriptManager.RegisterStartupScript(Me, GetType(Page), "ShowPasswordReset", "ShowPasswordReset();", True)
        End Sub
    End Class
End Namespace