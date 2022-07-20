Namespace Predictathon.Pages.Competition
    Partial Public Class CompetitionDetailEdit
        Inherits Predictathon.Web.UI.Page

#Region "Properties"
        Protected Friend ReadOnly Property CompetitionID As Guid
            Get
                Return New Guid(Request.QueryString("CompetitionID"))
            End Get
        End Property
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "Competition Detail"

            If Not IsPostBack Then
                If Not Predictathon.UserManager.CurrentUser.CompetitionAdministrator Then
                    'user shouldn't be here - send 'em away
                    Response.Redirect("~/Pages/Common/MainMenu.aspx", True)
                Else
                    LoadCompetitionDetails()
                End If
            End If

            divSaveConfirmed.Visible = False
        End Sub

        Protected Sub LoadCompetitionDetails()
            Dim objCompetition As PredictathonModel.Competition = Predictathon.CompetitionManager.Load(Me.CompetitionID)
            With objCompetition
                txtCompetitionName.Text = .CompetitionName
                chkPrependNameWithThe.Checked = .PrependNameWithThe
                dteStartDate.Value = .StartDate
                dteEndDate.Value = .EndDate
                chkDuplicateFixturesAllowed.Checked = .DuplicateFixturesAllowed
                chkDefaultToNeutralGround.Checked = .DefaultToNeutralGround
                chkAllowTwoPointers.Checked = .AllowTwoPointers
                chkOpenForRegistration.Checked = .OpenForRegistration
                chkRegistrationAvailableOnLoginPage.Checked = .RegistrationAvailableOnLoginPage
                chkShowInHallOfFame.Checked = .ShowInHallOfFame
                txtEntranceFee.Text = .EntranceFee.ToString
                chkPayPalPaymentAvailable.Checked = .PayPalPaymentAvailable
                txtImageFilename.Text = .ImageFilename
                txtInformation.Text = .Information
            End With
        End Sub

        Protected Sub SaveCompetitionDetails()
            Dim objCompetition As PredictathonModel.Competition = Predictathon.CompetitionManager.Load(Me.CompetitionID)
            With objCompetition
                .CompetitionName = txtCompetitionName.Text
                .PrependNameWithThe = chkPrependNameWithThe.Checked
                .StartDate = dteStartDate.Value
                .EndDate = dteEndDate.Value
                .DuplicateFixturesAllowed = chkDuplicateFixturesAllowed.Checked
                .DefaultToNeutralGround = chkDefaultToNeutralGround.Checked
                .AllowTwoPointers = chkAllowTwoPointers.Checked
                .OpenForRegistration = chkOpenForRegistration.Checked
                .RegistrationAvailableOnLoginPage = chkRegistrationAvailableOnLoginPage.Checked
                .ShowInHallOfFame = chkShowInHallOfFame.Checked
                .EntranceFee = CDec(txtEntranceFee.Text)
                .PayPalPaymentAvailable = chkPayPalPaymentAvailable.Checked
                .ImageFilename = txtImageFilename.Text
                .Information = txtInformation.Text
            End With

            CompetitionManager.Save(objCompetition)

            divSaveConfirmed.Visible = True
        End Sub

        Private Sub btnSubmitDetails_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSubmitDetails.Click
            If OnValidate() Then
                SaveCompetitionDetails()
            End If
        End Sub

        Private Function OnValidate() As Boolean
            Dim blnValid As Boolean = True

            blnValid = blnValid And Validation.ValidateControl(txtCompetitionName, lblCompetitionName, Validation.ValidationType.NullOrEmpty)
            blnValid = blnValid And Validation.ValidateControl(dteStartDate, lblStartDate, Validation.ValidationType.Date)
            blnValid = blnValid And Validation.ValidateControl(dteEndDate, lblEndDate, Validation.ValidationType.Date)

            ' Maximum entrance fee? £99.99
            If IsNumeric(txtEntranceFee.Text) Then
                If CDec(txtEntranceFee.Text) >= 100D Then
                    blnValid = False
                    Validation.RenderValidation(txtEntranceFee, False, lblEntranceFee, "Value must be less than £100.")
                Else
                    Validation.RenderValidation(txtEntranceFee, True, lblEntranceFee, String.Empty)
                End If
            Else
                blnValid = False
                Validation.RenderValidation(txtEntranceFee, False, lblEntranceFee, "Value must be numeric.")
            End If

            Return blnValid
        End Function

        Private Sub btnChange_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnChange.Click
            CompetitionManager.CurrentCompetitionID = Me.CompetitionID
            Response.Redirect("~/Pages/Common/MainMenu.aspx")
        End Sub

        Private Sub btnShowTeams_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnShowTeams.Click
            Response.Redirect("~/Pages/Competition/CompetitionTeamList.aspx?CompetitionID=" & Me.CompetitionID.ToString)
        End Sub
    End Class
End Namespace