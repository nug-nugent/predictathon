Namespace Predictathon.UserControls.Competition
    Public Class UserCompetitionRegistrationList
        Inherits Predictathon.Web.UI.UserControl

#Region " Properties "
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
                RebuildList()
                'hide the whole control if there's only one competition in the list

                If gvUserCompetitionRegistration.Rows.Count <= 1 Then
                    Me.Visible = False
                End If
            End If
        End Sub

        Private Sub RebuildList()
            'return a list of competitions either open for registration, or which the user is signed up to
            gvUserCompetitionRegistration.DataSource = Predictathon.CompetitionManager.UserCompetitionRegistrationListGet(Me.UserID)
            gvUserCompetitionRegistration.DataBind()
        End Sub

        Private Sub gvUserCompetitionRegistration_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvUserCompetitionRegistration.RowCommand
            If e.CommandName = "EditRecord" Then
                'is the user already registered for the selected competition?
                Dim gdCompetitionID As Guid = gvUserCompetitionRegistration.DataKeyValue(e).Value
                If IsNothing(Predictathon.UserCompetitionManager.Load(UserManager.CurrentUserID, gdCompetitionID)) Then
                    'not registered - redirect to registration page
                    Response.Redirect("~/Pages/User/UserCompetitionRegistration.aspx?CompetitionID=" & gdCompetitionID.ToString, False)
                    HttpContext.Current.ApplicationInstance.CompleteRequest()
                Else
                    'registered - change the current user's CompetitionID and make it the user's default competition
                    CompetitionManager.CurrentCompetitionID = gdCompetitionID
                    UserCompetitionManager.SetDefaultCompetitionForUser(UserManager.CurrentUserID, gdCompetitionID)

                    'redirect to the current URL
                    Response.Redirect(HttpContext.Current.Request.Url.AbsoluteUri, False)
                    HttpContext.Current.ApplicationInstance.CompleteRequest()
                End If
            End If
        End Sub

        Private Sub gvUserCompetitionRegistration_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvUserCompetitionRegistration.PageIndexChanging
            gvUserCompetitionRegistration.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Protected Function ImageURL(ByVal ImageName As String) As String
            Return If(String.IsNullOrEmpty(ImageName), "~/Images/Common/spacer.gif", "~/Images/Competitions/" & ImageName).ToString
        End Function

        Private Sub gvUserCompetitionRegistration_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles gvUserCompetitionRegistration.RowDataBound
            If e.Row.RowType = DataControlRowType.DataRow Then
                'set the text and CssClass of lblRegistrationStatus
                Dim objResult As PredictathonModel.UserCompetitionRegistrationListGet_Result = DirectCast(e.Row.DataItem, PredictathonModel.UserCompetitionRegistrationListGet_Result)
                Dim lblRegistrationStatus As Label = DirectCast(e.Row.FindControl("lblRegistrationStatus"), Label)
                If objResult.Registered Then
                    lblRegistrationStatus.CssClass = "Yes"
                    lblRegistrationStatus.Text = "Registered<br />Click here to select"
                Else
                    lblRegistrationStatus.CssClass = "No"
                    lblRegistrationStatus.Text = String.Format("{0} {1}<br />Entry fee: {2}<br />Click here to register", _
                                                               If(objResult.StartDate <= Date.Today, "Started", "Starts"), _
                                                               objResult.StartDate.ToString("dd/MM/yyyy"), _
                                                               objResult.EntranceFee.ToString("C2"))
                End If
            End If
        End Sub
    End Class
End Namespace