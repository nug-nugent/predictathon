Option Strict Off
Namespace Predictathon
    Public Class Validation
        Public Enum ValidationType
            [NullOrEmpty]
            [Date]
            [Numeric]
            [Email]
            [PastDate]
            [FutureDate]
        End Enum

        Public Shared Function RenderValidation(ByVal control As Object, ByVal IsValid As Boolean, ByVal lbl As Label, ByVal Message As String) As Boolean
            With control
                If Not IsValid Then
                    .BackColor = Drawing.Color.FromName("#EE3600")
                    If Not IsNothing(lbl) Then
                        If lbl.Text.Contains("<") Then
                            lbl.Text = lbl.Text.Split("<").FirstOrDefault
                        End If
                        lbl.Text = lbl.Text + " <strong style=""color:#EE3600""> *" & Message & " </strong>"
                    End If
                    Return False
                Else
                    If Not IsNothing(lbl) Then lbl.Text = lbl.Text.Split("<").FirstOrDefault
                    .BackColor = Drawing.Color.White
                End If
            End With
            Return True
        End Function

        Public Shared Function ValidateControl(ByVal Control As Object, ByVal TextLabel As Label, ByVal ValidateFor As ValidationType, Optional ByVal Message As String = "") As Boolean
            Dim IsValid As Boolean = True
            Try
                Select Case Control.GetType.Name
                    Case "TextBox"
                        If Not String.IsNullOrEmpty(Control.Text.Trim) Then
                            Select Case ValidateFor
                                Case ValidationType.Email
                                    IsValid = IsEmailAddress(Control.Text)
                                    Message = "Invalid email address"
                                Case ValidationType.Numeric
                                    IsValid = IsNumeric(Control.Text)
                                    Message = "Value must be numeric"
                            End Select
                        Else
                            IsValid = False
                        End If
                    Case "DatePicker"
                        IsValid = DirectCast(Control, Web.UI.WebControls.DatePicker).HasValidValue
                    Case "TimePicker"
                        IsValid = DirectCast(Control, Web.UI.WebControls.TimePicker).HasValidValue
                    Case "DropDownList"
                        IsValid = Not String.IsNullOrEmpty(Control.SelectedValue) AndAlso Control.SelectedValue <> Guid.Empty.ToString
                End Select
            Catch ex As Exception
                IsValid = False
            End Try

            If String.IsNullOrEmpty(Message) Then
                Message = "Required"
            End If

            Return RenderValidation(Control, IsValid, TextLabel, Message)
        End Function

        Public Shared Function IsEmailAddress(ByVal text As String) As Boolean
            Return New Regex("^([0-9a-zA-Z]([-\.\w]*[0-9a-zA-Z])*@([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,9})$").IsMatch(text)
        End Function
    End Class
End Namespace