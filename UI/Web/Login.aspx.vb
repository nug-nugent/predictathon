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

        Private Sub Login_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
            hdnCurrentServerDateTime.Value = CommonMethods.LongDateAndTimeString(Now)
        End Sub

        Private Sub RebuildList()
            gvCompetitions.DataSource = CompetitionManager.CompetitionListForLoginPageGet()
            gvCompetitions.DataBind()
            lblOpenForRegistration.Visible = gvCompetitions.Rows.Count > 0
        End Sub

        Private Function ValidateInput() As Boolean
            Dim blnValid As Boolean = True

            'Username
            If txtUsername.Text.Trim = String.Empty Then
                lblUsernameError.Visible = True
                blnValid = False
            Else
                lblUsernameError.Visible = False
            End If

            'Password
            If txtPassword.Text.Trim = String.Empty Then
                lblPasswordError.Visible = True
                blnValid = False
            Else
                lblPasswordError.Visible = False
            End If

            Return blnValid
        End Function

        Private Sub AttemptLogin()
            If Predictathon.Security.Authenticate(txtUsername.Text, txtPassword.Text, chkRememberMe.Checked) Then
                'login successful
                Response.Redirect("~/Pages/Common/MainMenu.aspx")
            Else
                lblError.Visible = True
            End If
        End Sub

        Private Sub gvCompetitions_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvCompetitions.RowCommand
            If e.CommandName = "ViewRecord" Then
                Response.Redirect("~/Pages/User/UserRegistration.aspx?CompetitionID=" & gvCompetitions.DataKeyValue(e).Value.ToString)
            End If
        End Sub

        Private Sub btnLogIn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLogIn.Click
            If ValidateInput() Then AttemptLogin()
        End Sub

        Private Sub gvCompetitions_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles gvCompetitions.RowDataBound
            'check whether the competition's open for registration
            If e.Row.RowType = DataControlRowType.DataRow Then
                Dim objCompetition As PredictathonModel.Competition = DirectCast(e.Row.DataItem, PredictathonModel.Competition)
                If String.IsNullOrEmpty(hdnCompetitionID.Value) Then hdnCompetitionID.Value = objCompetition.CompetitionID.ToString
                Dim lblRegistrationStatus As Label = DirectCast(e.Row.FindControl("lblRegistrationStatus"), Label)
                If objCompetition.OpenForRegistration Then
                    lblRegistrationStatus.Text = "Registration open - sign up here"
                    lblRegistrationStatus.CssClass = "Yes"
                Else
                    lblRegistrationStatus.Text = "Registration closed"
                    lblRegistrationStatus.CssClass = "No"
                End If
            End If
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