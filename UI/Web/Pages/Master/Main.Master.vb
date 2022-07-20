Namespace Predictathon.Master
    Public Class Main
        Inherits System.Web.UI.MasterPage

#Region " Properties "
        Public WriteOnly Property TitleSuffix() As String
            Set(ByVal value As String)
                If Not String.IsNullOrEmpty(value) Then
                    Title1.Text &= " - " & value
                End If
            End Set
        End Property

        Private Sub Main_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
            hdnCurrentServerDateTime.Value = CommonMethods.LongDateAndTimeString(Now)
        End Sub
#End Region

    End Class
End Namespace