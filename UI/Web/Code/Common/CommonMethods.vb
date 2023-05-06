'TODO - priority 1 - Trophies!
'TODO - priority 1 - All-time league table
'TODO - priority 2 - Lock navigation (and browser close?) when prediction remains unsaved
'TODO - priority 2 - Prediction page - put timer event on home team score change, to avoid changes being double-logged when away team score entered
'TODO - priority 2 - League table arrows - comparison date
'TODO - priority 4 - SEO
'TODO - priority 4 - Contact us/feedback
'TODO - priority 5 - Add fixture list to TeamDetail page.
'TODO - priority 7 - 'As it stands' league table, for latest scores...!

Namespace Predictathon
    Public Class CommonMethods
        ''' <summary>
        ''' '1 = 1st, 2 = 2nd, 3 = 3rd etc etc.
        ''' </summary>
        ''' <param name="Value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function IntegerToOrdinal(ByVal Value As Integer) As String
            If Value = 0 Then Return "0"
            Select Case (Value Mod 100)
                Case 11, 12, 13
                    Return Value.ToString & "th"
            End Select
            Select Case (Value Mod 10)
                Case 1
                    Return Value.ToString & "st"
                Case 2
                    Return Value.ToString & "nd"
                Case 3
                    Return Value.ToString & "rd"
                Case Else
                    Return Value.ToString & "th"
            End Select
        End Function

        Public Shared Function IsImage(ByVal FileName As String) As Boolean
            Dim strFileExtension As String = System.IO.Path.GetExtension(FileName).ToLower
            If strFileExtension = ".jpg" _
            OrElse strFileExtension = ".jpeg" _
            OrElse strFileExtension = ".gif" _
            OrElse strFileExtension = ".png" _
            OrElse strFileExtension = ".bmp" Then
                Return True
            Else
                Return False
            End If

        End Function

#Region " Date-related methods "
        Public Shared Function LongDateAndTimeString(ByVal value As DateTime, Optional ByVal IncludeTime As Boolean = True) As String
            Dim strDate As String = value.DayOfWeek.ToString & " " & CommonMethods.IntegerToOrdinal(value.Day) & " " & value.ToString("MMMM") & " " & value.Year.ToString
            Dim strTime As String = ""
            If IncludeTime Then strTime = " " & value.ToString("HH:mm")
            Return strDate & strTime
        End Function

        Public Shared Function ShortDateString(ByVal value As DateTime) As String
            Return CommonMethods.IntegerToOrdinal(value.Day) & " " & value.ToString("MMM")
        End Function

        Public Shared Function LastMilliSecondOfDay(ByVal Value As Date) As Date
            Return If(Value = Date.MinValue, Date.MinValue, Value.Date.AddHours(24).AddMilliseconds(-1))
        End Function

        ''' <summary>
        ''' Finds the time difference between two strings and returns eg "3 days", "6 hours", "1 hour and 32 minutes", "10 minutes", or "1 minute and 26 seconds"
        ''' dependent on the extent of the difference
        ''' </summary>
        ''' <param name="StartDate"></param>
        ''' <param name="EndDate"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function DateDifferenceString(ByVal StartDate As Date, ByVal EndDate As Date) As String
            Dim strDifference As String = ""
            Dim intDays As Long = DateDiff(DateInterval.Day, StartDate, EndDate)

            If intDays > 7 Then
                Dim intWeeks As Integer = CInt(Math.Round(intDays / 7, 0))
                If intWeeks > 8 Then
                    Dim intMonths As Long = DateDiff(DateInterval.Month, StartDate, EndDate)
                    strDifference = intMonths.ToString & String.Format(" month{0}", If(intMonths = 1, "", "s"))
                Else
                    strDifference = intWeeks.ToString & String.Format(" week{0}", If(intWeeks = 1, "", "s"))
                End If
            ElseIf intDays >= 4 Then
                strDifference = intDays.ToString & " days"
            ElseIf intDays > 0 Then
                Dim intHours As Long = DateDiff(DateInterval.Hour, StartDate.AddDays(intDays), EndDate)
                strDifference = intDays.ToString & String.Format(" day{0}, ", If(intDays = 1, "", "s")) & intHours.ToString & String.Format(" hour{0}", If(intHours = 1, "", "s"))
            End If

            If intDays = 0 Then
                Dim intHours As Long = DateDiff(DateInterval.Hour, StartDate, EndDate)
                Dim intMinutes As Long = DateDiff(DateInterval.Minute, StartDate.AddHours(intHours), EndDate)

                If intHours >= 2 Then
                    strDifference = intHours.ToString & " hours"
                ElseIf intHours = 1 Then
                    strDifference = "1 hour, " & intMinutes.ToString & String.Format(" minute{0}", If(intMinutes = 1, "", "s"))
                Else
                    If intMinutes > 0 Then strDifference = intMinutes.ToString & String.Format(" minute{0}", If(intMinutes = 1, "", "s"))
                    If intMinutes <= 5 Then
                        Dim intSeconds As Long = DateDiff(DateInterval.Second, StartDate.AddMinutes(intMinutes), EndDate)
                        strDifference &= If(intMinutes = 0, "", ", ") & intSeconds.ToString & String.Format(" second{0}", If(intSeconds = 1, "", "s"))
                    End If
                End If
            End If
            Return strDifference
        End Function

        Public Shared Function NextDayOfWeekDate(ByVal StartDate As Date, ByVal DayOfWeek As DayOfWeek) As Date
            Return StartDate.AddDays(DaysToAdd(StartDate.DayOfWeek, DayOfWeek))
        End Function

        Private Shared Function DaysToAdd(ByVal CurrentDay As DayOfWeek, ByVal DesiredDay As DayOfWeek) As Integer
            ' f( c, d ) = g( c, d ) mod 7, g( c, d ) > 7 
            '           = g( c, d ), g( c, d ) < = 7 
            '   where 0 <= c < 7 and 0 <= d < 7 
            Dim c As Integer = CInt(CurrentDay)
            Dim d As Integer = CInt(DesiredDay)
            Dim n As Integer = (7 - c + d)
            Return If((n > 7), n Mod 7, n)
        End Function

        ''' <summary>
        ''' Sends an email
        ''' </summary>
        ''' <param name="EmailTo">Recipient address</param>
        ''' <param name="cc">CC recipient</param>
        ''' <param name="BCC">BCC recipient</param>
        ''' <param name="subject">Email subject</param>
        ''' <param name="body">Email body</param>
        Public Shared Sub SendEmail(EmailTo As String, CC As String, BCC As String, Subject As String, Body As String)
            ' Instantiate a new instance of MailMessage and set the recipients
            Dim objMailMessage As New System.Net.Mail.MailMessage()
            With objMailMessage
                ' Add our recipients
                .To.Add(New System.Net.Mail.MailAddress(EmailTo))
                If Not String.IsNullOrEmpty(CC) Then .CC.Add(New System.Net.Mail.MailAddress(CC))
                If Not String.IsNullOrEmpty(BCC) Then .Bcc.Add(New System.Net.Mail.MailAddress(BCC))
                ' Set the subject, body, format (always HTML), and priority (always normal)
                .Subject = Subject
                .Body = Body
                .IsBodyHtml = True
                .Priority = System.Net.Mail.MailPriority.Normal
            End With

            ' Instantiate a new instance of SmtpClient and send the message
            Dim objSmtpClient As New System.Net.Mail.SmtpClient()
            objSmtpClient.Send(objMailMessage)
        End Sub

        ''' <summary>
        ''' Returns e.g. https://predictathon.co.uk/ or https://localhost/Predictathon/
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function CurrentURLRoot() As String
            Return HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) & VirtualPathUtility.ToAbsolute("~/")
        End Function
#End Region

    End Class
End Namespace
