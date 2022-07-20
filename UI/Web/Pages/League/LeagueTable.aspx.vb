Namespace Predictathon.Pages.League
    Partial Public Class LeagueTable
        Inherits Predictathon.Web.UI.Page

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "League Table"

            ' Hide the 2-pointer column?
            gvLeagueTable.Columns(4).Visible = CompetitionManager.CurrentCompetition.AllowTwoPointers

            If Not IsPostBack Then
                If Request.QueryString("Date") = "LastWeek" Then
                    hdnSearchMode.Value = "FromTo"
                    dteFrom.Value = MatchManager.PreviousMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID, MatchManager.MatchWeekStartDate(Date.Today), True)
                    dteTo.Value = dteFrom.Value.AddDays(6)
                ElseIf Request.QueryString("Date") = "ThisWeek" Then
                    Dim dteStart As Date = MatchManager.PreviousMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID, MatchManager.MatchWeekStartDate(Date.Today))
                    dteStart = MatchManager.NextMatchWeekEndDateGet(CompetitionManager.CurrentCompetitionID, dteStart.AddDays(7))
                    hdnSearchMode.Value = "FromTo"
                    dteFrom.Value = MatchManager.MatchWeekStartDate(dteStart)
                    dteTo.Value = dteStart.AddDays(6)
                End If

                ddlMonth_Bind()
                RebuildList()
            End If

            If hdnSearchMode.Value = "Month" Then
                spnFromTo.Style("display") = "none"
                spnMonth.Style("display") = "inline"
            Else
                spnMonth.Style("display") = "none"
                spnFromTo.Style("display") = "inline"
            End If
        End Sub

        Private Sub RebuildList()
            If hdnSearchMode.Value = "Month" Then
                'ddlMonth.SelectedValue is in the format of "Month_Year"
                Dim intMonth As Integer = CInt(ddlMonth.SelectedValue.Split("_"c)(0))
                If intMonth = 0 AndAlso dteFrom.Value = Date.MinValue AndAlso dteTo.Value = Date.MinValue Then
                    'show everything
                    gvLeagueTable.DataSource = Predictathon.MatchManager.LeagueTableGet(CompetitionManager.CurrentCompetitionID, Nothing, Nothing, Date.Today)
                ElseIf intMonth > 0 Then
                    'show data for a single month
                    Dim intYear As Integer = CInt(ddlMonth.SelectedValue.Split("_"c)(1))
                    Dim dteFrom As Date = CDate("01/" & intMonth & "/" & intYear.ToString)
                    gvLeagueTable.DataSource = Predictathon.MatchManager.LeagueTableGet(CompetitionManager.CurrentCompetitionID, dteFrom, dteFrom.AddMonths(1).AddDays(-1), Nothing)
                End If
            Else
                gvLeagueTable.DataSource = Predictathon.MatchManager.LeagueTableGet(CompetitionManager.CurrentCompetitionID, dteFrom.Value, dteTo.Value, Nothing)
            End If
            gvLeagueTable.DataBind()
            gvLeagueTable.Columns(1).Visible = blnShowProgressArrow
        End Sub

        Private Sub btnSearch_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSearch.Click
            RebuildList()
        End Sub

        Private Sub ddlMonth_Bind()
            'build a list of all months from the start of the competition to now
            Dim dteFirstMatchDate As Date = MatchManager.FirstMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID)
            Dim intFirstMonth As Integer = dteFirstMatchDate.Month
            Dim intFirstYear As Integer = dteFirstMatchDate.Year
            Dim intCurrentMonth As Integer = DateTime.Today.Month
            Dim intCurrentYear As Integer = DateTime.Today.Year

            ddlMonth.Items.Add(New ListItem With {.Value = "0", .Text = "All matches"})

            While intFirstMonth <> intCurrentMonth
                ddlMonth.Items.Add(New ListItem With {.Value = CStr(intFirstMonth) & "_" & CStr(intFirstYear), .Text = MonthName(intFirstMonth) & " " & CStr(intFirstYear)})
                intFirstMonth += 1
                If intFirstMonth > 12 Then
                    intFirstMonth = 1
                    intFirstYear += 1
                End If
            End While
            ddlMonth.Items.Add(New ListItem With {.Value = CStr(intCurrentMonth) & "_" & CStr(intCurrentYear), .Text = MonthName(intCurrentMonth) & " " & CStr(intCurrentYear)})
        End Sub

        Private Sub gvLeagueTable_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvLeagueTable.RowCommand
            If e.CommandName = "ViewRecord" Then
                Response.Redirect("~/Pages/User/UserDetail.aspx?UserID=" & gvLeagueTable.DataKeyValue(e).Value.ToString)
            End If
        End Sub

        Private Sub ddlMonth_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlMonth.SelectedIndexChanged
            RebuildList()
        End Sub

        Private Sub btnNextWeek_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnNextWeek.Click
            If dteFrom.Value = Date.MinValue Then dteFrom.Value = Date.Today

            'advance dteDateFrom's value by 7 days, then find the next match week's end date and subtract 6 days from it..(!)
            dteFrom.Value = MatchManager.NextMatchWeekEndDateGet(CompetitionManager.CurrentCompetitionID, dteFrom.Value.AddDays(7), True)
            If Not dteFrom.Value = Date.MinValue Then
                dteFrom.Value = dteFrom.Value.AddDays(-6)
                dteTo.Value = dteFrom.Value.AddDays(6)
                'databind the grid
                RebuildList()
            Else
                'we can't go that far ahead... find the final match week's start date instead
                btnNextWeek.Enabled = False
                btnNextMonth.Enabled = False
                dteFrom.Value = MatchManager.FinalMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID, True)
                dteTo.Value = dteFrom.Value.AddDays(6)
                'databind the grid
                RebuildList()
            End If

            btnPreviousWeek.Enabled = True
            btnPreviousMonth.Enabled = True
        End Sub

        Private Sub btnNextMonth_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnNextMonth.Click
            If dteFrom.Value = Date.MinValue Then dteFrom.Value = Date.Today

            'advance dteDateFrom's value by 1 month, then find the next match week's end date and subtract 6 days from it..(!)
            dteFrom.Value = MatchManager.NextMatchWeekEndDateGet(CompetitionManager.CurrentCompetitionID, dteFrom.Value.AddMonths(1), True)
            If Not dteFrom.Value = Date.MinValue Then
                dteFrom.Value = dteFrom.Value.AddDays(-6)
                dteTo.Value = dteFrom.Value.AddDays(6)
                'databind the grid
                RebuildList()
            Else
                'we can't go that far ahead... find the final match week's start date instead
                btnNextWeek.Enabled = False
                btnNextMonth.Enabled = False
                dteFrom.Value = MatchManager.FinalMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID, True)
                dteTo.Value = dteFrom.Value.AddDays(6)
                'databind the grid
                RebuildList()
            End If

            btnPreviousWeek.Enabled = True
            btnPreviousMonth.Enabled = True
        End Sub

        Private Sub btnPreviousWeek_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnPreviousWeek.Click
            If dteFrom.Value = Date.MinValue Then dteFrom.Value = Date.Today

            'find the previous week's start date
            dteFrom.Value = MatchManager.PreviousMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID, dteFrom.Value.AddDays(-1), True)
            If Not dteFrom.Value = Date.MinValue Then
                dteTo.Value = dteFrom.Value.AddDays(6)
                'databind the grid
                RebuildList()
            Else
                'we can't go back that far... find the first match date instead
                btnPreviousWeek.Enabled = False
                btnPreviousMonth.Enabled = False
                dteFrom.Value = MatchManager.FirstMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID)
                dteTo.Value = dteFrom.Value.AddDays(6)
                'databind the grid
                RebuildList()
            End If

            btnNextWeek.Enabled = True
            btnNextMonth.Enabled = True
        End Sub

        Private Sub btnPreviousMonth_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnPreviousMonth.Click
            If dteFrom.Value = Date.MinValue Then dteFrom.Value = Date.Today

            'subtract a month from dteDateFrom, then find the previous week's start date
            dteFrom.Value = MatchManager.PreviousMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID, dteFrom.Value.AddMonths(-1), True)
            If Not dteFrom.Value = Date.MinValue Then
                dteTo.Value = dteFrom.Value.AddDays(6)
                'databind the grid
                RebuildList()
            Else
                'we can't go back that far... find the first match date instead
                btnPreviousWeek.Enabled = False
                btnPreviousMonth.Enabled = False
                dteFrom.Value = MatchManager.FirstMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID)
                dteTo.Value = dteFrom.Value.AddDays(6)
                'databind the grid
                RebuildList()
            End If

            btnNextWeek.Enabled = True
            btnNextMonth.Enabled = True
        End Sub

        Private blnUserHighlighted As Boolean = False
        Private blnShowProgressArrow As Boolean = False
        Private Sub gvLeagueTable_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles gvLeagueTable.RowDataBound
            'highlight the current user in the grid
            If e.Row.RowType = DataControlRowType.DataRow Then
                If blnUserHighlighted = False AndAlso New Guid(gvLeagueTable.DataKeys(e.Row.RowIndex).Value.ToString) = UserManager.CurrentUserID Then
                    e.Row.CssClass = "GridViewRowHighlight"
                    blnUserHighlighted = True
                End If

                Dim objDataItem As PredictathonModel.LeagueTableGet_Result = DirectCast(e.Row.DataItem, PredictathonModel.LeagueTableGet_Result)
                If objDataItem.PreviousLeaguePosition.HasValue Then
                    blnShowProgressArrow = True
                End If

                DirectCast(e.Row.Cells(0).FindControl("imgProgress"), Image).ImageUrl = ProgressArrowImage(CInt(objDataItem.LeaguePosition.Value), objDataItem.PreviousLeaguePosition)
            End If
        End Sub

        Protected Function ProgressArrowImage(ByVal CurrentPosition As Integer, ByVal PreviousPosition As Integer?) As String
            If PreviousPosition.HasValue Then
                If PreviousPosition.Value = CurrentPosition Then
                    'league position is unchanged
                    Return CommonMethods.CurrentURLRoot & "Images/Common/NoChange.gif"
                ElseIf PreviousPosition.Value < CurrentPosition Then
                    'league position has worsened
                    Return CommonMethods.CurrentURLRoot & "Images/Common/DownArrow.gif"
                Else
                    'league position has improved
                    Return CommonMethods.CurrentURLRoot & "Images/Common/UpArrow.gif"
                End If
            Else
                Return CommonMethods.CurrentURLRoot & "Images/Common/Spacer.gif"
            End If
        End Function
    End Class
End Namespace