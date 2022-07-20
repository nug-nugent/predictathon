Namespace Predictathon.UserControls
    Public Class Navigation
        Inherits Predictathon.Web.UI.UserControl

        Private Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            Dim objCurrentUser As PredictathonModel.User = UserManager.CurrentUser
            hypEditProfile.HRef = "~/Pages/User/UserDetailEdit.aspx?UserID=" & objCurrentUser.UserID.ToString

            ' Is the current user permitted to see all of the options?
            liMessageboard.Visible = objCurrentUser.CanViewMessageboard

            Dim intAdminOptionsVisible As Integer = 0

            If Not objCurrentUser.MatchAdministrator Then
                ' Hide the match administration options
                liMatchListAdmin.Visible = False
                liProcessMatches.Visible = False
            Else
                intAdminOptionsVisible += 1
            End If

            If Not objCurrentUser.UserAdministrator Then
                ' Hide the payment credit option
                liPaymentCreditList.Visible = False
            Else
                intAdminOptionsVisible += 1
            End If

            If Not objCurrentUser.CompetitionAdministrator Then
                ' Hide the competition administration option
                liCompetitionList.Visible = False
            Else
                intAdminOptionsVisible += 1
            End If

            If intAdminOptionsVisible = 0 Then
                ' No admin options are available - hide the admin section in its entirety
                liAdministration.Visible = False
            End If
        End Sub
    End Class
End Namespace