Namespace Predictathon
	Public Class UserManager
		Inherits Predictathon.Manager
		Public Const EntitySetName As String = "Users"

#Region "Properties"
        Public Shared Property CurrentUserID() As Guid
            Get
                If Not IsNothing(HttpContext.Current.Session("CurrentUserID")) Then
                    Return New Guid(HttpContext.Current.Session("CurrentUserID").ToString)
                Else
                    ' No current user! The following method may save us...
                    If Predictathon.Security.InitialiseSessionFromCurrentIdentity() Then
                        Return New Guid(HttpContext.Current.Session("CurrentUserID").ToString)
                    Else
                        ' Failure - whichever method is calling this will need to deal with this
                        Return Guid.Empty
                    End If
                End If
            End Get
            Set(ByVal value As Guid)
                HttpContext.Current.Session("CurrentUserID") = value
                'while we're here, ensure a CurrentCompetitionID is set
                SetCurrentCompetitionIDForUser(value)
            End Set
        End Property

        Public Shared ReadOnly Property CurrentUser() As PredictathonModel.User
			Get
				Return Load(CurrentUserID)
			End Get
		End Property
#End Region

		Public Shared Function Load(ByVal Username As String) As PredictathonModel.User
			Using objContext As New PredictathonModel.PredictathonEntities
				Return objContext.Users.FirstOrDefault(Function(User) User.Username = Username)
			End Using
		End Function

		Public Shared Function LoadByEmailAddress(ByVal EmailAddress As String) As PredictathonModel.User
			Using objContext As New PredictathonModel.PredictathonEntities
				Return objContext.Users.FirstOrDefault(Function(User) User.EmailAddress = EmailAddress)
			End Using
		End Function

		Public Shared Function Load(ByVal UserID As Guid) As PredictathonModel.User
			Using objContext As New PredictathonModel.PredictathonEntities
				Return objContext.Users.FirstOrDefault(Function(User) User.UserID = UserID)
			End Using
		End Function

		Public Shared Sub Delete(ByVal User As PredictathonModel.User)
			Data.Delete(User, EntitySetName)
		End Sub

		Public Shared Sub Save(ByVal User As PredictathonModel.User)
			Data.Save(User, EntitySetName)
		End Sub

		Public Shared Function Create(ByVal UserID As Guid, ByVal Username As String) As PredictathonModel.User
			Return New PredictathonModel.User With {.UserID = UserID, .Username = Username, .CanViewMessageboard = True}
		End Function

		Public Shared Function UserOverduePredictionsGet() As List(Of PredictathonModel.UserOverduePredictionsGet_Result)
			Using objContext As New PredictathonModel.PredictathonEntities
				Return objContext.UserOverduePredictionsGet.ToList
			End Using
		End Function

		Public Shared Sub SetCurrentCompetitionIDForUser(ByVal UserID As Guid)
			'find the default competition for the user
			Using objContext As New PredictathonModel.PredictathonEntities
				Dim objUserCompetition As PredictathonModel.UserCompetition = objContext.UserCompetitions.Where(Function(UserCompetition) UserCompetition.UserID = UserID And UserCompetition.IsDefaultCompetition = True).FirstOrDefault
				If Not IsNothing(objUserCompetition) Then
					CompetitionManager.CurrentCompetitionID = objUserCompetition.CompetitionID
				Else
					'no default competition for this user! Something's wrong - look for another competition instead and make it the default
					Dim objUserCompetitionNotDefault As PredictathonModel.UserCompetition = objContext.UserCompetitions.Where(Function(UserCompetition) UserCompetition.UserID = UserID).FirstOrDefault
					If Not IsNothing(objUserCompetitionNotDefault) Then
						CompetitionManager.CurrentCompetitionID = objUserCompetitionNotDefault.CompetitionID
						Predictathon.UserCompetitionManager.SetDefaultCompetitionForUser(objUserCompetitionNotDefault.UserID, objUserCompetitionNotDefault.CompetitionID)
					End If
				End If
			End Using
		End Sub

		''' <summary>
		''' Creates a User based on the values in a Transaction record, then updates and saves the Transaction.
		''' </summary>
		''' <param name="Transaction"></param>
		''' <remarks></remarks>
		Public Shared Sub CreateAndAuthenticateUserFromTransaction(ByVal Transaction As PredictathonModel.Transaction, ByVal SendEmailConfirmation As Boolean)
			'create a User record
			Dim objUser As PredictathonModel.User = UserManager.Create(Guid.NewGuid, Transaction.Username)
			With objUser
				.EmailAddress = Transaction.EmailAddress
				.Forenames = Transaction.Forenames
				.Surname = Transaction.Surname
				.Password = Transaction.Password
				Transaction.UserID = .UserID
			End With

			'create a UserCompetition record
			Dim objUserCompetition As PredictathonModel.UserCompetition = UserCompetitionManager.Create(Guid.NewGuid, objUser.UserID, Transaction.CompetitionID.Value)
			objUserCompetition.AmountPaid = If(Transaction.ActualPaymentAmount, 0D)
			Transaction.UserCompetitionID = objUserCompetition.UserCompetitionID
			If Not String.IsNullOrEmpty(Transaction.PayPalTransactionID) Then objUserCompetition.PaymentProvider = "PayPal"

			'save the records..
			UserManager.Save(objUser)
			UserCompetitionManager.Save(objUserCompetition)
			TransactionManager.Save(Transaction)

			'set the new competition to be the user's default competition
			UserCompetitionManager.SetDefaultCompetitionForUser(objUser.UserID, objUserCompetition.CompetitionID)

			'..authenticate the user we've just created
			Security.Authenticate(objUser.Username, String.Empty, False, True)

			If SendEmailConfirmation Then
				UserManager.SendEmailConfirmation(objUserCompetition.UserCompetitionID)
			End If
		End Sub

		''' <summary>
		''' Sends email confirmation that a user has signed up for a competition
		''' </summary>
		''' <param name="UserCompetitionID"></param>
		''' <remarks></remarks>
		Public Shared Sub SendEmailConfirmation(ByVal UserCompetitionID As Guid)
			Dim objUserCompetition As PredictathonModel.UserCompetition = UserCompetitionManager.Load(UserCompetitionID)
			If Not IsNothing(objUserCompetition) Then
				Dim objUser As PredictathonModel.User = UserManager.Load(objUserCompetition.UserID)
				Dim objCompetition As PredictathonModel.Competition = CompetitionManager.Load(objUserCompetition.CompetitionID)

				'build up the email subject and body based on the entities we've just loaded
				Dim strSubject As String = "Predictathon - " & objCompetition.CompetitionName
				Dim strBody As New System.Text.StringBuilder
				strBody.Append("<html>").AppendLine()
				strBody.Append("    <body style='font-size: small; font-family:arial,helvetica,sans-serif'>").AppendLine()
				strBody.Append("        <table style=""width: 100%; height: 84px; border: 0;"" border=""0"" cellpadding=""0"" cellspacing=""0"">").AppendLine()
				strBody.Append("            <tr>").AppendLine()
				strBody.Append("                <td align=""center"">").AppendLine()
				strBody.Append("                    <img alt=""Predictathon"" src=""").Append(CommonMethods.CurrentURLRoot & "Images/Branding/PredictathonBrain.gif").Append(""" />").AppendLine()
				strBody.Append("                </td>").AppendLine()
				strBody.Append("            </tr>").AppendLine()
				strBody.Append("        </table>").AppendLine()
				strBody.Append("        <div style=""margin: 0pt auto; width: 550px; text-align: left; padding: 0pt 15px; margin-bottom: 15px;"">").AppendLine()
				strBody.Append("            <br />Dear ").Append(objUser.Forenames).Append(",<br /><br />").AppendLine()
				strBody.Append("            Thanks for signing up for the ").Append(objCompetition.CompetitionName).Append(" Predictathon.").AppendLine()
				strBody.Append("            Your account is now active; you can access it by visiting <a href='").Append(CommonMethods.CurrentURLRoot).Append("'>predictathon.co.uk</a> and entering the login details below.").AppendLine()
				strBody.Append("            <br /><br />").AppendLine()
				strBody.Append("            Username: ").Append(objUser.Username).Append("<br />").AppendLine()
				strBody.Append("            Password: (starts with '").Append(Security.Encryption.Decrypt(objUser.Password).Substring(0, 1)).Append("')<br /><br />").AppendLine()
				strBody.Append("            <strong>What happens next?</strong>").AppendLine()
				strBody.Append("            <br /><br />").AppendLine()
				strBody.Append("            <ul>").AppendLine()
				strBody.Append("                <li>Log on to the site and enter your first predictions via the 'Predictions' menu.</li>").AppendLine()
				strBody.Append("                <li>Update your profile, add an image, and set email reminders via the 'edit profile' menu option.</li>").AppendLine()
				strBody.Append("                <li>Keep up to date with your progress via the league table, which is updated every match day.</li>").AppendLine()
				strBody.Append("                <li>Keep predicting throughout the competition to be in with a chance of winning").Append(If(objCompetition.EntranceFee > 0D, " a cash prize", "")).Append(".</li>").AppendLine()
				strBody.Append("            </ul>").AppendLine()
				strBody.Append("            Good luck and have fun,").AppendLine()
				strBody.Append("            <br /><br />").AppendLine()
				strBody.Append("            Predictathon.").AppendLine()
				strBody.Append("        </div>").AppendLine()
				strBody.Append("        <table style=""border-top: 1px solid black; width: 100%; margin-bottom: 15px;"">").AppendLine()
				strBody.Append("            <tr>").AppendLine()
				strBody.Append("                <td style=""width: 100%; height: 28px; background-color: #3E95FF; text-align: center;"">").AppendLine()
				strBody.Append("                    <div style=""font-size: 12px; font-weight: bold; color: #000000"">This automated email was sent by predictathon.co.uk as your email address was entered on the site.</div>").AppendLine()
				strBody.Append("                </td>").AppendLine()
				strBody.Append("            </tr>").AppendLine()
				strBody.Append("        </table>").AppendLine()
				strBody.Append("    </body>").AppendLine()
				strBody.Append("</html>")

				'send the email, including a BCC address of our choice from web.config (none if it's blank)
				CommonMethods.SendEmail(objUser.EmailAddress, "", ConfigurationManager.AppSettings("EmailSignUpBCCAddress"), strSubject, strBody.ToString)
			End If
		End Sub

		Public Shared Sub ResetPassword(ByVal User As PredictathonModel.User, ByVal SendByEmail As Boolean)
			'reset the user's password, save the record, and optionally send an email confirming the new password
			User.Password = Predictathon.Security.Encryption.Encrypt(Predictathon.Security.RandomCharacterStringGet(6))
			Save(User)

			If SendByEmail Then
				SendPasswordResetEmail(User)
			End If
		End Sub

		Public Shared Sub SendPasswordResetEmail(ByVal User As PredictathonModel.User)
			Dim strSubject As String = "Predictathon"

			'build up the body of the email
			Dim strBody As New System.Text.StringBuilder
			strBody.Append("<html>").AppendLine()
			strBody.Append("    <body style='font-size: small; font-family:arial,helvetica,sans-serif'>").AppendLine()
			strBody.Append("        <table style=""width: 100%; height: 84px; border: 0;"" border=""0"" cellpadding=""0"" cellspacing=""0"">").AppendLine()
			strBody.Append("            <tr>").AppendLine()
			strBody.Append("                <td align=""center"">").AppendLine()
			strBody.Append("                    <img alt=""Predictathon"" src=""").Append(CommonMethods.CurrentURLRoot & "Images/Branding/PredictathonBrain.gif").Append(""" />").AppendLine()
			strBody.Append("                </td>").AppendLine()
			strBody.Append("            </tr>").AppendLine()
			strBody.Append("        </table>").AppendLine()
			strBody.Append("        <div style=""margin: 0pt auto; width: 550px; text-align: left; padding: 0pt 15px; margin-bottom: 15px;"">").AppendLine()
			strBody.Append("            <br />Dear ").Append(User.Username).Append(",<br /><br />").AppendLine()
			strBody.Append("            A password reset has just been requested for your account. Your new password is <strong>").Append(Security.Encryption.Decrypt(User.Password)).Append("</strong>.").AppendLine()
			strBody.Append("            <br /><br />").AppendLine()
			strBody.Append("            Please visit <a href='").Append(CommonMethods.CurrentURLRoot).Append("'>predictathon.co.uk</a> and log in. You can change your password via the 'Edit User' option.").AppendLine()
			strBody.Append("            <br /><br />").AppendLine()
			strBody.Append("            Happy predicting,").AppendLine()
			strBody.Append("            <br /><br />").AppendLine()
			strBody.Append("            Predictathon.").AppendLine()
			strBody.Append("        </div>").AppendLine()
			strBody.Append("        <table style=""border-top: 1px solid black; width: 100%; margin-bottom: 15px;"">").AppendLine()
			strBody.Append("            <tr>").AppendLine()
			strBody.Append("                <td style=""width: 100%; height: 28px; background-color: #3E95FF; text-align: center;"">").AppendLine()
			strBody.Append("                    <div style=""font-size: 12px; font-weight: bold; color: #000000"">This automated email was sent by predictathon.co.uk as your email address or username was entered on the site.</div>").AppendLine()
			strBody.Append("                </td>").AppendLine()
			strBody.Append("            </tr>").AppendLine()
			strBody.Append("        </table>").AppendLine()
			strBody.Append("    </body>").AppendLine()
			strBody.Append("</html>")

			'send the email
			CommonMethods.SendEmail(User.EmailAddress, "", "", strSubject, strBody.ToString)
		End Sub

		''' <summary>
		''' To be run daily. Sends an email to all users/competitions on which a prediction is due within the user's chosen email reminder timeframe
		''' </summary>
		''' <remarks></remarks>
		Public Shared Sub SendPredictionEmailReminder()
			'get a list of all users/competitions on which a prediction is due within the user's chosen email reminder timeframe
			Dim gdLastUser As Guid = Guid.Empty
			Dim lstUserOverduePredictions As List(Of PredictathonModel.UserOverduePredictionsGet_Result) = UserManager.UserOverduePredictionsGet
			For Each objResult In lstUserOverduePredictions
				'there may be multiple results for this user, if they're due to predict in more than one competition...
				If gdLastUser <> objResult.UserID Then
					Dim gdUserID As Guid = objResult.UserID	'because the lambda expression gets funny about the iteration result being passed in...
					'send an email to the user, potentially taking into account multiple competitions...
					SendPredictionEmailReminder(lstUserOverduePredictions.Where(Function(result) result.UserID = gdUserID).ToList)
				End If

				'continue the loop
				gdLastUser = objResult.UserID
			Next
		End Sub

		''' <summary>
		''' Sends an email to a single user for all competitions with overdue predictions in the supplied list
		''' </summary>
		''' <param name="UserOverduePredictions"></param>
		''' <remarks></remarks>
		Protected Shared Sub SendPredictionEmailReminder(ByVal UserOverduePredictions As List(Of PredictathonModel.UserOverduePredictionsGet_Result))
			Dim objUser As PredictathonModel.User = UserManager.Load(UserOverduePredictions.FirstOrDefault.UserID)

            If objUser IsNot Nothing Then
                Dim subject As String = "Prediction reminder"
                'build up the body of the email
                Dim strBody As New System.Text.StringBuilder
                strBody.Append("<html>").AppendLine()
                strBody.Append("    <body style='font-size: small; font-family:arial,helvetica,sans-serif'>").AppendLine()
                strBody.Append("        <table style=""width: 100%; height: 84px; border: 0;"" border=""0"" cellpadding=""0"" cellspacing=""0"">").AppendLine()
                strBody.Append("            <tr>").AppendLine()
                strBody.Append("                <td align=""center"">").AppendLine()
                strBody.Append("                    <img alt=""Predictathon"" src=""").Append(CommonMethods.CurrentURLRoot & "Images/Branding/PredictathonBrain.gif").Append(""" />").AppendLine()
                strBody.Append("                </td>").AppendLine()
                strBody.Append("            </tr>").AppendLine()
                strBody.Append("        </table>").AppendLine()
                strBody.Append("        <div style=""margin: 0pt auto; width: 550px; text-align: left; padding: 0pt 15px; margin-bottom: 15px;"">").AppendLine()
                strBody.Append("            <br />Dear ").Append(objUser.Username).Append(",<br /><br />").AppendLine()
                strBody.Append("            You need to make a prediction in the following competition").Append(If(UserOverduePredictions.Count > 1, "s", "")).Append(":").AppendLine()
                strBody.Append("            <br /><br />").AppendLine()
                strBody.Append("            <table style=""width: 100%"">").AppendLine()
                strBody.Append("                <tr>").AppendLine()
                strBody.Append("                    <td style=""width: 50%""><strong>Competition</strong></td>").AppendLine()
                strBody.Append("                    <td style=""width: 50%""><strong>Next prediction due</strong></td>").AppendLine()
                strBody.Append("                </tr>").AppendLine()
                For Each objOverdue In UserOverduePredictions
                    strBody.Append("                <tr>").AppendLine()
                    strBody.Append("                    <td>").Append(objOverdue.CompetitionName).Append("</td>").AppendLine()
                    strBody.Append("                    <td>").Append(CommonMethods.LongDateAndTimeString(objOverdue.NextPredictionDue.Value)).Append("</td>").AppendLine()
                    strBody.Append("                </tr>").AppendLine()
                Next
                strBody.Append("            </table>").AppendLine()
                strBody.Append("            <br /><br />").AppendLine()
				strBody.Append("            Please visit <a href='").Append(CommonMethods.CurrentURLRoot).Append("Pages/Match/Predictions.aspx").Append("'>predictathon.co.uk</a> and log in to register your predictions.").AppendLine()
				strBody.Append("            <br /><br />").AppendLine()
                strBody.Append("            Good luck,").AppendLine()
                strBody.Append("            <br /><br />").AppendLine()
                strBody.Append("            Predictathon.").AppendLine()
                strBody.Append("        </div>").AppendLine()
                strBody.Append("        <table style=""border-top: 1px solid black; width: 100%; margin-bottom: 15px;"">").AppendLine()
                strBody.Append("            <tr>").AppendLine()
                strBody.Append("                <td style=""width: 100%; height: 28px; background-color: #3E95FF; text-align: center;"">").AppendLine()
                strBody.Append("                    <div style=""font-size: 12px; font-weight: bold; color: #000000"">You can change the regularity of these reminders in your Predictathon account.</div>").AppendLine()
                strBody.Append("                </td>").AppendLine()
                strBody.Append("            </tr>").AppendLine()
                strBody.Append("        </table>").AppendLine()
                strBody.Append("    </body>").AppendLine()
                strBody.Append("</html>")

                Dim blnSuccess As Boolean = True

                Try
                    'send the email
                    CommonMethods.SendEmail(objUser.EmailAddress, "", "", subject, strBody.ToString)
                Catch ex As Exception
                    'Something went wrong. We don't want to update the last sent date/time of the affected records:
                    blnSuccess = False
                    Elmah.ErrorSignal.FromCurrentContext.Raise(ex) 'log the error
                End Try

                If blnSuccess Then
                    'the email was sent. Set the last email reminder sent date/time for the relevant UserCompetitions to now
                    For Each objOverdue In UserOverduePredictions
                        Dim objUserCompetition As PredictathonModel.UserCompetition = UserCompetitionManager.Load(objOverdue.UserCompetitionID)
                        objUserCompetition.LastEmailReminderSent = Date.Now
                        UserCompetitionManager.Save(objUserCompetition)
                    Next
                End If
            End If
		End Sub
	End Class
End Namespace