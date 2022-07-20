Namespace Predictathon.Pages.Payment
    Partial Public Class PaymentConfirmation
        Inherits Predictathon.Web.UI.Page

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                ProcessPayPalFormPost()
            ElseIf Me.IsPostBack AndAlso Request.Form("__EVENTTARGET") = tdLogin.UniqueID Then
                'the header image has been clicked
                Response.Redirect("~/Login.aspx", False)
            End If
        End Sub

        Private Sub ProcessPayPalFormPost()
            Dim strResponse As String = PayPalResponseGet()
            If Not strResponse.ToLower.StartsWith("success") Then
                'the post failed... have another try in case it was a connection problem
                strResponse = PayPalResponseGet()
            End If

            Dim objCompetition As PredictathonModel.Competition = Nothing 'load the record later
            Dim blnNewUserRegistered As Boolean

            If strResponse.ToLower.StartsWith("success") Then
                'we have a response from PayPal. Now we can parse the rest of the form data
                'firstly get everything bar the 'success'status
                strResponse = Mid(strResponse, strResponse.IndexOf(vbLf) + 2)

                'now split that response (by line feed) into a string array
                Dim strResponseKeys As String() = Split(strResponse, vbLf)
                Dim intKeys As Integer = UBound(strResponseKeys) - 1

                Dim gdTransactionID As Guid = Guid.Empty
                Dim strReceiverID As String = ""
                Dim intPaymentAmount As Decimal = 0D
                Dim intPayPalFee As Decimal = 0D
                Dim strPayPalTransactionID As String = ""
                Dim strPaymentStatus As String = ""

                'loop through the keys until the array is exhausted
                For i As Integer = 0 To intKeys
                    Dim strValueKey As String() = Split(strResponseKeys(i), "=")
                    If strValueKey(0) = "receiver_id" Then
                        strReceiverID = If(IsNothing(strValueKey(1)), "", strValueKey(1))
                    ElseIf strValueKey(0) = "mc_gross" Then
                        Decimal.TryParse(If(IsNothing(strValueKey(1)), "", strValueKey(1)), intPaymentAmount)
                    ElseIf strValueKey(0) = "mc_fee" Then
                        Decimal.TryParse(If(IsNothing(strValueKey(1)), "", strValueKey(1)), intPayPalFee)
                    ElseIf strValueKey(0) = "txn_id" Then
                        strPayPalTransactionID = If(IsNothing(strValueKey(1)), "", strValueKey(1))
                    ElseIf strValueKey(0) = "payment_status" Then
                        strPaymentStatus = If(IsNothing(strValueKey(1)), "", strValueKey(1))
                    ElseIf strValueKey(0) = "custom" Then
                        Guid.TryParse(If(IsNothing(strValueKey(1)), "", strValueKey(1)), gdTransactionID)
                    End If
                Next

                'Hopefully, we now have our values. Now we have to validate them.
                Dim blnValid As Boolean = True
                If gdTransactionID = Guid.Empty Then
                    'problem - we can't link this back to a transaction. This should never, EVER, happen!
                    blnValid = False
                Else
                    Dim objTransaction As PredictathonModel.Transaction = TransactionManager.Load(gdTransactionID)

                    'Has the PayPal transaction (txn_id) already been used?
                    If Not TransactionManager.IsValidPayPalTransactionID(gdTransactionID, strPayPalTransactionID) Then
                        blnValid = False
                        objTransaction.Comments &= "Duplicate PayPal txn_id. "
                    End If

                    'check the receiver_id and amount were correct
                    If strReceiverID <> ConfigurationManager.AppSettings("PayPalMerchantAccountID") Then
                        blnValid = False
                        objTransaction.Comments &= "Incorrect PayPal receiver_id. "
                    End If

                    If intPaymentAmount <> objTransaction.Amount Then
                        blnValid = False
                        objTransaction.Comments &= "Incorrect payment amount. "
                    End If

                    objTransaction.ContainedInvalidData = Not blnValid

                    'now populate the Transaction record with PayPal's txn_id etc
                    objTransaction.PayPalTransactionID = strPayPalTransactionID
                    objTransaction.PayPalFee = intPayPalFee
                    objTransaction.ActualPaymentAmount = intPaymentAmount
                    objTransaction.TransactionStatus = strPaymentStatus

                    'The possible values of payment_status, as of 16/11/2011:
                    'Canceled_Reversal: A reversal has been canceled. For example, you won a dispute with the customer, and the funds for the transaction that was reversed have been returned to you.
                    'Completed: The payment has been completed, and the funds have been added successfully to your account balance.
                    'Created: A German ELV payment is made using Express Checkout.
                    'Denied: You denied the payment. This happens only if the payment was previously pending because of possible reasons described for the pending_reason variable or the Fraud_Management_Filters_x variable.
                    'Expired: This authorization has expired and cannot be captured.
                    'Failed: The payment has failed. This happens only if the payment was made from your customer’s bank account.
                    'Pending: The payment is pending. See pending_reason for more information.
                    'Refunded: You refunded the payment.
                    'Reversed: A payment was reversed due to a chargeback or other type of reversal. The funds have been removed from your account balance and returned to the buyer. The reason for the reversal is specified in the ReasonCode element.
                    'Processed: A payment has been accepted.
                    'Voided: This authorization has been voided.
                    objTransaction.Failed = Not (strPaymentStatus = "Completed" OrElse strPaymentStatus = "Pending" OrElse strPaymentStatus = "Processed")
                    If objTransaction.Failed Then blnValid = False

                    TransactionManager.Save(objTransaction)

                    If blnValid Then
                        'create the User and UserCompetition records
                        If Not objTransaction.UserID.HasValue AndAlso IsNothing(UserManager.Load(objTransaction.Username)) Then
                            UserManager.CreateAndAuthenticateUserFromTransaction(objTransaction, True)
                            blnNewUserRegistered = True
                        ElseIf Not objTransaction.UserCompetitionID.HasValue Then
                            'the user already exists - they're therefore signing up for an additional competition.
                            UserCompetitionManager.CreateUserCompetitionFromTransaction(objTransaction)
                            blnNewUserRegistered = False
                        End If
                    End If

                    'load the Competition record
                    objCompetition = Predictathon.CompetitionManager.Load(objTransaction.CompetitionID.Value)
                End If

                If blnValid Then
                    If blnNewUserRegistered Then
                        divPaymentSuccessfulNewUser.Visible = True
                        liKeepPredicting.InnerText = String.Format("Keep predicting throughout the competition to be in with a chance of winning{0}.", If(objCompetition.EntranceFee > 0D, " a cash prize", ""))
                    Else
                        divPaymentSuccessfulExistingUser.Visible = True
                        liKeepPredicting2.InnerText = String.Format("Keep predicting throughout the competition to be in with a chance of winning{0}.", If(objCompetition.EntranceFee > 0D, " a cash prize", ""))
                    End If
                Else
                    divPaymentFailed.Visible = True
                End If
                Else
                    'PayPal's response wasn't "success", ie there was a problem
                    divPaymentFailed.Visible = True
                End If
        End Sub

        Protected Function PayPalResponseGet() As String
            'PayPal have just posted us a 'transaction token':
            Dim strTransactionToken As String = Request.QueryString("tx")

            'PayPal-related variables from web.config
            Dim strPostURL As String = ConfigurationManager.AppSettings("PayPalPostURL") 'the page we're posting this form to
            Dim strAuthToken As String = ConfigurationManager.AppSettings("PayPalAuthToken") 'our PayPal auth token for 'PDT'

            'Post the value back to them, and they'll tell us how the transaction went.
            Dim strQuery As String = String.Format("cmd=_notify-synch&tx={0}&at={1}", strTransactionToken, strAuthToken)
            Dim objWebRequest As System.Net.WebRequest = System.Net.WebRequest.Create(strPostURL)
            With objWebRequest
                .Method = "POST"
                .ContentType = "application/x-www-form-urlencoded"
                .ContentLength = strQuery.Length
            End With
            Dim objStreamWriter As System.IO.StreamWriter = New System.IO.StreamWriter( _
                                                            objWebRequest.GetRequestStream(), _
                                                            System.Text.Encoding.ASCII)
            objStreamWriter.Write(strQuery)
            objStreamWriter.Close()

            'Make the request to PayPal and get the response
            Dim objStreamReader As System.IO.StreamReader = New System.IO.StreamReader(objWebRequest.GetResponse.GetResponseStream)
            Dim strResponse As String = objStreamReader.ReadToEnd
            objStreamReader.Close()

            Return strResponse
        End Function

        Private Sub btnLogin_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLogin.Click
            Response.Redirect("~/Pages/Common/MainMenu.aspx")
        End Sub

        Private Sub btnLogin2_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLogin2.Click
            Response.Redirect("~/Pages/Common/MainMenu.aspx")
        End Sub
    End Class
End Namespace