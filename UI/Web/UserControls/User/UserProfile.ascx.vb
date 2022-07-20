Namespace Predictathon.UserControls.User
    Public Class UserProfile
        Inherits Predictathon.Web.UI.UserControl

#Region "Properties"
        Private _UserID As Guid
        Public Property UserID() As Guid
            Get
                Return _UserID
            End Get
            Set(ByVal value As Guid)
                _UserID = value
            End Set
        End Property
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                DisplayUserProfile()

                If (Not Me.UserID = Predictathon.UserManager.CurrentUserID AndAlso Not Predictathon.UserManager.CurrentUser.UserAdministrator) Then
                    btnEditUser.Visible = False
                End If
            End If
        End Sub

        Protected Sub DisplayUserProfile()
            Dim objUser As PredictathonModel.User = Predictathon.UserManager.Load(UserID)
            lblUsername.Text = objUser.Username

            If objUser.ImageUploaded Then
                imgProfile.ImageUrl = "~/Uploads/Images/" & Me.UserID.ToString & "_sm.jpg"
                imgLarge.ImageUrl = "~/Uploads/Images/" & Me.UserID.ToString & ".jpg"
                'attach JavaScript to show larger image on click
                imgProfile.Attributes("onclick") = "ShowPopup();"
                imgProfile.Attributes("style") &= "; cursor: pointer"
            Else
                imgProfile.ImageUrl = "~/Images/Common/NoImageAvailable.gif"
            End If

            Dim blnProfileDetailExists As Boolean = False
            If Not String.IsNullOrEmpty(objUser.Caption) Then
                lblCaption.Text = objUser.Caption
                blnProfileDetailExists = True
            Else
                pCaption.Visible = False
            End If

            If Not String.IsNullOrEmpty(objUser.FavouriteTeam) Then
                lblFavouriteTeamValue.Text = objUser.FavouriteTeam
                blnProfileDetailExists = True
            Else
                pFavouriteTeam.Visible = False
            End If

            If Not String.IsNullOrEmpty(objUser.Location) Then
                lblLocationValue.Text = objUser.Location
                blnProfileDetailExists = True
            Else
                pLocation.Visible = False
            End If

            If Not String.IsNullOrEmpty(objUser.ProfileText) Then
                lblProfileText.Text = objUser.ProfileText
                blnProfileDetailExists = True
            Else
                pProfileText.Visible = False
            End If

            pNoInfoAvailable.Visible = Not blnProfileDetailExists
        End Sub

        Private Sub btnEditUser_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnEditUser.Click
            Response.Redirect("~/Pages/User/UserDetailEdit.aspx?UserID=" & Me.UserID.ToString, False)
        End Sub
    End Class
End Namespace