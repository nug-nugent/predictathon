Namespace Predictathon.Pages.Payment
    Partial Public Class PayPalIPNHandler
        Inherits Predictathon.Web.UI.Page

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                ProcessPayPalFormPost()
            End If
        End Sub

        Private Sub ProcessPayPalFormPost()
            'PayPal-related variables from web.config
            Dim strPostURL As String = ConfigurationManager.AppSettings("PayPalPostURL") 'the page we're posting this form to
            Dim strMerchantAccountID As String = ConfigurationManager.AppSettings("PayPalMerchantAccountID") 'our PayPal account code

            'PayPal have just posted us some form data
            'We now need to post it back to them with one new variable + the rest exactly as we got it, and they'll confirm that the data is real and valid.
            Dim Param() As Byte = Request.BinaryRead(HttpContext.Current.Request.ContentLength)
            Dim strRequest As String = Encoding.ASCII.GetString(Param) & "&cmd=_notify-validate"

            Dim objWebRequest As System.Net.WebRequest = System.Net.WebRequest.Create(strPostURL)
            With objWebRequest
                .Method = "POST"
                .ContentType = "application/x-www-form-urlencoded"
                .ContentLength = strRequest.Length
            End With

            'Send the request to PayPal and get the response
            Dim objStreamWriter As System.IO.StreamWriter = New System.IO.StreamWriter(objWebRequest.GetRequestStream, Encoding.ASCII)
            objStreamWriter.Write(strRequest)
            objStreamWriter.Close()
            Dim objStreamReader As System.IO.StreamReader = New System.IO.StreamReader(objWebRequest.GetResponse.GetResponseStream)
            Dim strResponse As String = objStreamReader.ReadToEnd
            objStreamReader.Close()

            If strResponse.ToLower = "verified" Then
                'it's good... but what's it telling us?
                'split the original response (by &) into a string array
                Dim strResponseKeys As String() = Split(strRequest, "&")
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

                'we've got the values... now deal with them
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
                        ElseIf Not objTransaction.UserCompetitionID.HasValue Then
                            'the user already exists - they're therefore signing up for an additional competition.
                            UserCompetitionManager.CreateUserCompetitionFromTransaction(objTransaction)
                        End If
                    End If
                End If
            Else
                'naughty naughty
            End If

        End Sub

    End Class
End Namespace