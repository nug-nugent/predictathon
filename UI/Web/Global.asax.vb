Imports Elmah
Imports System.Web.Optimization

Public Class Global_asax
	Inherits HttpApplication

	Public Sub Application_Start(sender As Object, e As EventArgs)
		Net.ServicePointManager.SecurityProtocol = Net.SecurityProtocolType.Tls Or Net.SecurityProtocolType.Tls11 Or Net.SecurityProtocolType.Tls12

		BundleConfig.RegisterBundles(BundleTable.Bundles)
		BundleTable.EnableOptimizations = True
	End Sub

	Public Sub ErrorMail_Filtering(sender As Object, e As ExceptionFilterEventArgs)
		If ShouldDismiss(e.Exception) Then
			e.Dismiss()
		End If
	End Sub

	Private Function ShouldDismiss(ex As Exception) As Boolean
		Dim baseException = If(ex?.GetBaseException(), Nothing)
		If baseException Is Nothing Then
			Return False
		End If

		' Dismiss some common non-actionable exceptions
		If TypeOf baseException Is ArgumentException Then Return True
		If TypeOf baseException Is FormatException Then Return True
		If TypeOf baseException Is HttpRequestValidationException Then Return True

		If TypeOf baseException Is HttpException Then
			Dim msg As String = If(baseException.Message, String.Empty).ToUpperInvariant()
			If msg.Contains("THIS IS AN INVALID SCRIPT RESOURCE REQUEST") Then Return True
			If msg.Contains("A POTENTIALLY DANGEROUS REQUEST.PATH") Then Return True
			If msg.Contains("A POTENTIALLY DANGEROUS REQUEST.FORM") Then Return True
			If msg.Contains("INVALID POSTBACK OR CALLBACK ARGUMENT") Then Return True
			If msg.Contains("INVALID LENGTH FOR A BASE-64 CHAR ARRAY OR STRING") Then Return True
		End If

		Return False
	End Function
End Class