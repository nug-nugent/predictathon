Imports Elmah

Public Class Global_asax
	Inherits HttpApplication

	Public Sub Application_Start(sender As Object, e As EventArgs)
		Net.ServicePointManager.SecurityProtocol = Net.SecurityProtocolType.Tls Or Net.SecurityProtocolType.Tls11 Or Net.SecurityProtocolType.Tls12
	End Sub

	Public Sub ErrorMail_Filtering(sender As Object, e As ExceptionFilterEventArgs)
		' We don't want emails in certain situations, e.g. "Invalid postback or callback argument", "Invalid length for a Base-64 char array or string"
		Dim baseException = e.Exception?.GetBaseException()

		If baseException Is Nothing _
		OrElse TypeOf baseException Is ArgumentException _
		OrElse TypeOf baseException Is FormatException _
		OrElse (TypeOf baseException Is HttpException AndAlso baseException.Message.ToUpper.Contains("THIS IS AN INVALID SCRIPT RESOURCE REQUEST")) Then
			e.Dismiss()
		End If
	End Sub
End Class