Namespace Predictathon
	Public Class Security
		Public Shared Function Authenticate(UsernameOrEmailAddress As String, Password As String, Optional PersistentLogin As Boolean = False, Optional BypassPassword As Boolean = False) As Boolean
			Dim objUser As PredictathonModel.User = If(Validation.IsEmailAddress(UsernameOrEmailAddress),
				UserManager.LoadByEmailAddress(UsernameOrEmailAddress),
				UserManager.Load(UsernameOrEmailAddress)
			)

			If objUser IsNot Nothing Then
				' We have a user - does the password match?
				If BypassPassword OrElse Encryption.Decrypt(objUser.Password) = Password Then
					'set the forms authentication cookie, with behaviour dependent on whether this should be a persistent cookie
					If PersistentLogin Then
						Dim objAuthTicket As New FormsAuthenticationTicket(objUser.Username, True, 50000) 'make the forms authentication time out in a month or so (50000 minutes = 34 days)
						Dim strEncryptedTicket As String = FormsAuthentication.Encrypt(objAuthTicket)
						Dim objCookie As New HttpCookie(FormsAuthentication.FormsCookieName, strEncryptedTicket) With {.Expires = objAuthTicket.Expiration}
						HttpContext.Current.Response.Cookies.Set(objCookie)
					Else
						FormsAuthentication.SetAuthCookie(objUser.Username, False)
					End If

					objUser.LastLoginDateTime = DateTime.Now
					UserManager.CurrentUserID = objUser.UserID
					UserManager.Save(objUser)
					Return True
				Else
					' Authentication failed
					objUser = Nothing
					Return False
				End If
			Else
				' User doesn't exist
				Return False
			End If
		End Function

		Friend Shared Sub CheckForIdentityUserMismatch()
			If Not String.IsNullOrEmpty(HttpContext.Current.User?.Identity?.Name) Then
				If UserManager.Load(HttpContext.Current.User.Identity.Name) Is Nothing Then
					' The username in the auth cookie is not in the database. The user's probably changed their username, so will need to log 
					' out and in again:
					Logout()
				End If
			End If
		End Sub

		Public Shared Function UserIsAuthenticated() As Boolean
			If HttpContext.Current.User.Identity.IsAuthenticated AndAlso UserManager.CurrentUserID = Guid.Empty Then
				' Set session values based on current username
				Return InitialiseSessionFromCurrentIdentity()
			ElseIf HttpContext.Current.User.Identity.IsAuthenticated Then
				Return True
			Else
				Return False
			End If
		End Function

		Public Shared Function InitialiseSessionFromCurrentIdentity() As Boolean
			If Not String.IsNullOrEmpty(HttpContext.Current.User?.Identity?.Name) Then
				Dim objUser As PredictathonModel.User = UserManager.Load(HttpContext.Current.User.Identity.Name)
				If Not IsNothing(objUser) Then
					objUser.LastLoginDateTime = Date.Now
					UserManager.CurrentUserID = objUser.UserID
					UserManager.Save(objUser)

					Return True
				End If
			End If

			Return False
		End Function

		Public Shared Sub Logout()
			System.Web.Security.FormsAuthentication.SignOut()
			HttpContext.Current.Session.Abandon()
			HttpContext.Current.Response.Redirect("~/Login.aspx")
		End Sub

		Public Shared Function RandomCharacterStringGet(ByVal Length As Integer) As String
			Dim strRandom As String = ""
			While strRandom.Length < Length
				strRandom &= Guid.NewGuid.ToString.Replace("-", "")
			End While
			Return Left(strRandom, Length)
		End Function

		Protected Friend Class Encryption
			Private Const DESKey = "PRJHCXDZ" 'randomly-generated character strings to aid in our DES encryption - do not change!
			Private Shared DES As New System.Security.Cryptography.TripleDESCryptoServiceProvider
			Private Shared MD5 As New System.Security.Cryptography.MD5CryptoServiceProvider

			Protected Shared Function MD5Hash(ByVal value As String) As Byte()
				Return MD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(value))
			End Function

			Public Shared Function Encrypt(ByVal stringToEncrypt As String) As String
				DES.Key = MD5Hash(DESKey)
				DES.Mode = System.Security.Cryptography.CipherMode.ECB
				Dim Buffer As Byte() = ASCIIEncoding.ASCII.GetBytes(stringToEncrypt)
				Return Convert.ToBase64String(DES.CreateEncryptor().TransformFinalBlock(Buffer, 0, Buffer.Length))
			End Function

			Public Shared Function Decrypt(ByVal encryptedString As String) As String
				DES.Key = MD5Hash(DESKey)
				DES.Mode = System.Security.Cryptography.CipherMode.ECB
				Dim Buffer As Byte() = Convert.FromBase64String(encryptedString)
				Return ASCIIEncoding.ASCII.GetString(DES.CreateDecryptor().TransformFinalBlock(Buffer, 0, Buffer.Length))
			End Function
		End Class
	End Class
End Namespace