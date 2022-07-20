Namespace Predictathon.Pages.User
    Partial Public Class UserCompetitionRegistration
        Inherits Predictathon.Web.UI.Page

#Region "Properties"
        Private ReadOnly Property CompetitionID As Guid
            Get
                Return New Guid(Request.QueryString("CompetitionID"))
            End Get
        End Property
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                Dim objCompetition As PredictathonModel.Competition = CompetitionManager.Load(Me.CompetitionID)

                lblCompetitionNameValue.Text = objCompetition.CompetitionName
                lblStartDateValue.Text = objCompetition.StartDate.ToString("dd/MM/yyyy")
                If objCompetition.StartDate <= Date.Today Then lblStartDate.Text = "Started: "
                lblEndDateValue.Text = objCompetition.EndDate.ToString("dd/MM/yyyy")
                lblEntranceFeeValue.Text = objCompetition.EntranceFee.ToString("C2")

                If objCompetition.EntranceFee > 0 Then
                    lblEntranceFee.CssClass = "Error"
                    lblEntranceFeeValue.CssClass = "Error"
                End If

                lblCompetitionInformation.Text = objCompetition.Information
                pCompetitionInformation.Visible = Not String.IsNullOrEmpty(objCompetition.Information)
                liKeepPredicting.InnerText = String.Format("Keep predicting throughout the competition to be in with a chance of winning{0}.", If(objCompetition.EntranceFee > 0D, " a cash prize", ""))

                'is this competition open for registration?
                If Not objCompetition.OpenForRegistration Then
                    spnEntranceFee.Visible = False
                    divEnterCompetition.Visible = False
                    lblRegistrationStatus.Text = "Registration closed"
                    lblRegistrationStatus.CssClass = "No"
                Else
                    lblRegistrationStatus.Text = "Registration open"
                    lblRegistrationStatus.CssClass = "Yes"
                End If
            End If
        End Sub

        Private Sub btnEnterCompetition_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnEnterCompetition.Click
            divEnterCompetition.Visible = False

            'take payment if necessary
            If Predictathon.CompetitionManager.Load(Me.CompetitionID).EntranceFee > 0D Then
                ShowPaymentDetails()
            Else
                CreateUserCompetition(String.Empty)
            End If
        End Sub

        Private Sub ShowPaymentDetails()
            'load the competition the user is signing up for
            Dim objCompetition As PredictathonModel.Competition = Predictathon.CompetitionManager.Load(Me.CompetitionID)

            divPayment.Visible = True

            Dim intEntranceFee As Double = objCompetition.EntranceFee
            lblPaymentRequest.Text = String.Format("Thanks. To complete your registration, we require a one-off payment of £{0}.", intEntranceFee.ToString)

            If objCompetition.PayPalPaymentAvailable Then
                'PayPal fees are currently 3.4% + 20p
                Dim intPaypalPercentage As Decimal = 3.4D
                Dim intPaypalFlatFee As Decimal = 0.2D
                lblPaymentRequest2.Text = String.Format("This payment can be made by PayPal, which will remove, at the time of writing, {0} from the total prize fund.<br />" & _
                                          "This will not affect how much you pay.<br />" & _
                                          "Alternatively, if you've already been given an authorisation code, please enter it here to continue.", (((intEntranceFee / 100 * intPaypalPercentage)) + intPaypalFlatFee).ToString("C2"))

                rbPaymentCredit.Attributes("onclick") = "ShowPaymentOption('PaymentCredit');"
                rbPayPal.Attributes("onclick") = "ShowPaymentOption('PayPal');"
            Else
                'hide the payment options - the user can only pay by PaymentCredit code
                pPayPal.Visible = False
                pPaymentOptions.Visible = False
                lblPaymentRequest2.Text = "PayPal payment is not available for this competition. If you have an authorisation code, please enter it here."
            End If

            txtPaymentCreditCode.Focus()
        End Sub

        Private Sub CreateUserCompetition(ByVal PaymentCreditCode As String)
            'create a UserCompetition record
            Dim objUserCompetition As PredictathonModel.UserCompetition = UserCompetitionManager.Create(Guid.NewGuid, UserManager.CurrentUserID, Me.CompetitionID)
            objUserCompetition.IsDefaultCompetition = True

            If Not String.IsNullOrEmpty(PaymentCreditCode) Then
                'The user used a payment credit. Make sure it can't be used again.
                Dim objPaymentCredit As PredictathonModel.PaymentCredit = PaymentCreditManager.PaymentCreditListGet(Me.CompetitionID, PaymentCreditCode).FirstOrDefault
                If Not IsNothing(objPaymentCredit) Then
                    objUserCompetition.PaymentCreditID = objPaymentCredit.PaymentCreditID
                    objPaymentCredit.CreditUsed = True
                    objPaymentCredit.UsedByUserID = UserManager.CurrentUserID
                    PaymentCreditManager.Save(objPaymentCredit)
                End If
            End If

            UserCompetitionManager.Save(objUserCompetition)
            Predictathon.CompetitionManager.CurrentCompetitionID = Me.CompetitionID

            divPayment.Visible = False
            divRegistrationConfirmed.Visible = True
        End Sub

        Private Sub btnLogin_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLogin.Click
            Response.Redirect("~/Pages/Common/MainMenu.aspx")
        End Sub

        Private Sub btnSubmitPaymentCreditCode_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSubmitPaymentCreditCode.Click
            If Validation.ValidateControl(txtPaymentCreditCode, lblPaymentCreditCode, Validation.ValidationType.NullOrEmpty) = True Then
                'does the payment credit code exist? Is it valid for this competition?
                If PaymentCreditManager.IsValidPaymentCreditCode(Me.CompetitionID, txtPaymentCreditCode.Text) Then
                    'it's good...
                    CreateUserCompetition(txtPaymentCreditCode.Text)
                Else
                    'that code's no good here...
                    Validation.RenderValidation(txtPaymentCreditCode, False, lblPaymentCreditCode, "The code is invalid.")
                End If
            End If
        End Sub

        Private Sub btnPayPal_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnPayPal.Click
            'post a form to the PayPal website

            Dim objCompetition As PredictathonModel.Competition = Predictathon.CompetitionManager.Load(Me.CompetitionID)
            Dim strItemName As String = "Predictathon.co.uk - " & objCompetition.CompetitionName
            Dim intAmount As Decimal = objCompetition.EntranceFee

            ' Before we redirect the user to PayPal, create and save a Transaction record. The TransactionID will 
            ' come back to us from PayPal, so we'll know whether the user's payment was successful. The transaction also holds the
            ' details the user's just entered (username etc) so we can create the User and UserCompetition records when the
            ' transaction's success (hopefully!) status comes back to us.
            Dim objTransaction As PredictathonModel.Transaction = TransactionManager.Create(Guid.NewGuid)
            With objTransaction
                .Amount = intAmount
                .CompetitionID = Me.CompetitionID
                .UserID = Predictathon.UserManager.CurrentUserID
                .TransactionStatus = "Sent to PayPal"
            End With
            TransactionManager.Save(objTransaction)

            'PayPal-related variables from web.config
            Dim strPostURL As String = ConfigurationManager.AppSettings("PayPalPostURL") 'the page we're posting this form to
            Dim strMerchantAccountID As String = ConfigurationManager.AppSettings("PayPalMerchantAccountID") 'our PayPal account code
            Dim strAuthToken As String = ConfigurationManager.AppSettings("PayPalAuthToken") 'our PayPal auth token for 'PDT'
            Dim strReturnURL As String = Request.Url.GetLeftPart(UriPartial.Authority) & VirtualPathUtility.ToAbsolute("~/") & _
                "/Pages/Payment/PaymentConfirmation.aspx" 'the URL the user will be sent back to once they're done with PayPal

            'clear the current response as we're going to create and post a simple HTML form to PayPal 
            System.Web.HttpContext.Current.Response.Clear()
            System.Web.HttpContext.Current.Response.Write("<html><head>")
            System.Web.HttpContext.Current.Response.Write("</head><body onload=""document.form1.submit()"">")
            System.Web.HttpContext.Current.Response.Write("<form name=""form1"" method=""post"" action=""" & strPostURL & """>") 'this form will be submitted, by JavaScript, to PostURL

            'load the current user as we'll need to send a couple of details to PayPal
            Dim objUser As PredictathonModel.User = Predictathon.UserManager.CurrentUser

            'write the form values
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""cmd"" value=""_xclick"">") '"Pay now"
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""at"" value=""" & strAuthToken & """>") '"Auth token for PayPal's 'PDT' service
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""return"" value=""" & strReturnURL & """>") 'The URL PayPal will come back to
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""business"" value=""" & strMerchantAccountID & """>") 'Our PayPal account code
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""item_name"" value=""" & strItemName & """>") 'The name of the item (the competition name)
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""amount"" value=""" & intAmount.ToString("N2") & """>") 'the cost
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""no_shipping"" value=""1"">") 'we're not shipping
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""no_note"" value=""1"">") 'we don't need a note, thanks
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""currency_code"" value=""GBP"">") 'Eeenglish money,  please
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""first_name"" value=""" & Left(objUser.Forenames, 32) & """>") 'buyer's forenames (32 char limit)
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""last_name"" value=""" & objUser.Surname & """>") 'buyer's surname (64 char limit)
            System.Web.HttpContext.Current.Response.Write("<input type=""hidden"" name=""custom"" value=""" & objTransaction.TransactionID.ToString & """>") 'our TransactionID, which'll come back from PayPal's 'PDT' service

            'close the form and body 
            System.Web.HttpContext.Current.Response.Write("</form>")
            System.Web.HttpContext.Current.Response.Write("</body></html>")
            System.Web.HttpContext.Current.Response.End()
        End Sub
    End Class
End Namespace