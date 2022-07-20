Namespace Predictathon.Pages.Match
    Partial Public Class UserMatchPredictionList
        Inherits Predictathon.Web.UI.Page

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "Predictions"

            If Not IsPostBack AndAlso CheckForCallBack() = False Then
                dteDateFrom.Value = MatchManager.PreviousMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID, MatchManager.MatchWeekStartDate(Date.Today))
                If dteDateFrom.Value = Date.MinValue Then dteDateFrom.Value = MatchManager.MatchWeekStartDate(Date.Today)
                Dim dteDateTo As Date = MatchManager.NextMatchWeekEndDateGet(CompetitionManager.CurrentCompetitionID, Date.Today)
                'if dteDateFrom was more than 4 days ago, and dteDateto is more than 6 days ahead of date from, skip the first week(!)
                If dteDateFrom.Value <= Date.Today.AddDays(-7) AndAlso dteDateTo > dteDateFrom.Value.AddDays(6) Then dteDateFrom.Value = dteDateTo.AddDays(-6)

                'databind the grid
                Dim lstMatchPredictionList As List(Of PredictathonModel.UserMatchPredictionListGet_Result) = Predictathon.MatchManager.UserMatchPredictionListGet( _
                                                                Predictathon.UserManager.CurrentUserID, _
                                                                Predictathon.CompetitionManager.CurrentCompetitionID, _
                                                                dteDateFrom.Value, _
                                                                CommonMethods.LastMilliSecondOfDay(dteDateTo))

                'for the scenario in which no matches have yet occurred in the current competition, and therefore dteDateFrom.Value is potentially incorrect, set it to the first match in the list's MatchWeekStartDate
                If lstMatchPredictionList.Count > 0 Then
                    dteDateFrom.Value = Predictathon.MatchManager.MatchWeekStartDate(lstMatchPredictionList.First.MatchDateTime)
                End If

                lstMatch.DataSource = lstMatchPredictionList
                lstMatch.DataBind()
            End If
        End Sub

        Public Function CheckForCallBack() As Boolean
            Dim strCallBack As String = Request.Params("CallBack")
            If String.IsNullOrEmpty(strCallBack) Then
                Return False
            ElseIf strCallBack = "SavePrediction" Then
                SavePredictionFromCallBack()
            End If
            Return True
        End Function

        Private Sub SavePredictionFromCallBack()
            Dim gdMatchID As Guid
            Dim gdPredictionID As Guid?
            Dim intHomeTeamGoals As Integer = 0
            Dim intAwayTeamGoals As Integer = 0

            'the CallBack should have passed these values in Request.Params. If anything's gone wrong, don't call the Save method.
            Dim blnError As Boolean = False
            Try
                Guid.TryParse(Request.Params("MatchID"), gdMatchID)
                If Not String.IsNullOrEmpty(Request.Params("PredictionID")) Then gdPredictionID = New Guid(Request.Params("PredictionID"))
                Integer.TryParse(Request.Params("HomeTeamGoals"), intHomeTeamGoals)
                Integer.TryParse(Request.Params("AwayTeamGoals"), intAwayTeamGoals)
            Catch ex As Exception
                blnError = True
            End Try

            If Not blnError AndAlso SavePrediction(gdMatchID, gdPredictionID, intHomeTeamGoals, intAwayTeamGoals) = True Then
                Response.Write(Boolean.TrueString.ToLower)
                Response.End()
            Else
                Response.Write(Boolean.FalseString.ToLower)
                Response.End()
            End If
        End Sub

        Private Function SavePrediction(ByVal MatchID As Guid, ByVal PredictionID As Guid?, ByVal HomeTeamGoals As Integer, ByVal AwayTeamGoals As Integer) As Boolean
            'Ensure it's not too late to save the prediction
            Dim objMatch As PredictathonModel.Match = MatchManager.Load(MatchID)
            If Not objMatch.MatchDateTime <= DateAdd(DateInterval.Minute, 5, DateTime.Now) Then
                If Not PredictionID.HasValue Then
                    Dim objPrediction As PredictathonModel.Prediction = PredictionManager.Search(MatchID, Predictathon.UserManager.CurrentUserID).FirstOrDefault
                    If Not IsNothing(objPrediction) Then PredictionID = objPrediction.PredictionID
                End If

                Predictathon.PredictionManager.CreateAndSave(MatchID, _
                                                             PredictionID, _
                                                             Predictathon.UserManager.CurrentUserID, _
                                                             HomeTeamGoals, _
                                                             AwayTeamGoals)
                Return True
            Else
                Return False 'too late!
            End If
        End Function

        Private dteLastDateTime As DateTime = Nothing
        Private blnControlHasFocus As Boolean = False
        Private Sub lstMatch_ItemDataBound(ByVal sender As Object, ByVal e As ListViewItemEventArgs) Handles lstMatch.ItemDataBound
            If e.Item.ItemType = ListViewItemType.DataItem Then
                Dim objMatchPredictionResult As PredictathonModel.UserMatchPredictionListGet_Result = DirectCast(e.Item.DataItem, PredictathonModel.UserMatchPredictionListGet_Result)

                ' Only show the date/time header once per date/time
                If IsNothing(dteLastDateTime) OrElse dteLastDateTime <> objMatchPredictionResult.MatchDateTime Then
                    dteLastDateTime = objMatchPredictionResult.MatchDateTime
                    e.Item.FindControl("thDateTime").Visible = True
                Else
                    e.Item.FindControl("thDateTime").Visible = False
                End If

                If objMatchPredictionResult.Knockout Then
                    e.Item.FindControl("lblAsterisk").Visible = True
                    divKnockout.Visible = True
                End If

                If objMatchPredictionResult.MatchDateTime <= DateAdd(DateInterval.Minute, 5, DateTime.Now) Then
                    ' The match is scheduled to begin in 5 minutes or less. No changes allowed.
                    Dim txtHomeTeamGoals As TextBox = DirectCast(e.Item.FindControl("txtHomeTeamGoals"), TextBox)
                    Dim txtAwayTeamGoals As TextBox = DirectCast(e.Item.FindControl("txtAwayTeamGoals"), TextBox)
                    txtHomeTeamGoals.Enabled = False
                    txtAwayTeamGoals.Enabled = False
                    If String.IsNullOrEmpty(txtHomeTeamGoals.Text) Then txtHomeTeamGoals.Text = "L"
                    If String.IsNullOrEmpty(txtAwayTeamGoals.Text) Then txtAwayTeamGoals.Text = "L"

                    ' Has the match already been played and the result processed?
                    If objMatchPredictionResult.Score.HasValue Then
                        ' Yes - set a row style based on the success or otherwise of the prediction...
                        Dim intScore As Integer = objMatchPredictionResult.Score.Value
                        Dim lblScore As Label = DirectCast(e.Item.FindControl("lblScore"), Label)
                        lblScore.Text = intScore.ToString & String.Format(" point{0}", If(intScore = 1, "", "s"))
                        lblScore.CssClass &= " " & PredictionManager.GetCSSClassForScore(intScore)
                    Else
                        ' Tell the user it's too late to change their prediction
                        DirectCast(e.Item.FindControl("lblProgress"), Label).Text = "Awaiting result..."
                    End If
                Else
                    ' Add the necessary data- attributes to make handling the onkeypress event on our textboxes possible.
                    ' We'll automatically tab between textboxes, validate for numeric entry, and save the data via a CallBack when possible.
                    Dim txtHomeTeamGoals As TextBox = DirectCast(e.Item.FindControl("txtHomeTeamGoals"), TextBox)
                    Dim txtAwayTeamGoals As TextBox = DirectCast(e.Item.FindControl("txtAwayTeamGoals"), TextBox)
                    Dim lblProgress As Label = DirectCast(e.Item.FindControl("lblProgress"), Label)

                    txtHomeTeamGoals.Attributes.Add("data-linkedclientid", txtAwayTeamGoals.ClientID)
                    txtHomeTeamGoals.Attributes.Add("data-progressclientid", lblProgress.ClientID)
                    txtAwayTeamGoals.Attributes.Add("data-linkedclientid", txtHomeTeamGoals.ClientID)

                    ' Focus on the first enabled, non-populated textbox in the list
                    If Not blnControlHasFocus AndAlso String.IsNullOrEmpty(txtHomeTeamGoals.Text) Then
                        txtHomeTeamGoals.Focus()
                        blnControlHasFocus = True
                    End If
                End If
            End If
        End Sub

        Private Sub btnNextWeek_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnNextWeek.Click
            'advance dteDateFrom's value by 7 days, then find the next match week's end date and subtract 7 days from it..(!)
            dteDateFrom.Value = MatchManager.NextMatchWeekEndDateGet(CompetitionManager.CurrentCompetitionID, dteDateFrom.Value.AddDays(7))
            If Not dteDateFrom.Value = Date.MinValue Then
                dteDateFrom.Value = dteDateFrom.Value.AddDays(-6)
                'databind the grid
                lstMatch.DataSource = Predictathon.MatchManager.UserMatchPredictionListGet( _
                                                                Predictathon.UserManager.CurrentUserID, _
                                                                Predictathon.CompetitionManager.CurrentCompetitionID, _
                                                                dteDateFrom.Value, _
                                                                CommonMethods.LastMilliSecondOfDay(dteDateFrom.Value.AddDays(6)))
                lstMatch.DataBind()
            Else
                'we can't go that far ahead... find the final match week's start date instead
                btnNextWeek.Enabled = False
                btnNextMonth.Enabled = False
                dteDateFrom.Value = MatchManager.FinalMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID)
                'databind the grid
                lstMatch.DataSource = Predictathon.MatchManager.UserMatchPredictionListGet( _
                                                                Predictathon.UserManager.CurrentUserID, _
                                                                Predictathon.CompetitionManager.CurrentCompetitionID, _
                                                                dteDateFrom.Value, _
                                                                CommonMethods.LastMilliSecondOfDay(dteDateFrom.Value.AddDays(6)))
                lstMatch.DataBind()
            End If

            btnPreviousWeek.Enabled = True
            btnPreviousMonth.Enabled = True
        End Sub

        Private Sub btnNextMonth_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnNextMonth.Click
            'advance dteDateFrom's value by 1 month, then find the next match week's end date and subtract 7 days from it..(!)
            dteDateFrom.Value = MatchManager.NextMatchWeekEndDateGet(CompetitionManager.CurrentCompetitionID, dteDateFrom.Value.AddMonths(1))
            If Not dteDateFrom.Value = Date.MinValue Then
                dteDateFrom.Value = dteDateFrom.Value.AddDays(-6)
                'databind the grid
                lstMatch.DataSource = Predictathon.MatchManager.UserMatchPredictionListGet( _
                                                                Predictathon.UserManager.CurrentUserID, _
                                                                Predictathon.CompetitionManager.CurrentCompetitionID, _
                                                                dteDateFrom.Value, _
                                                                CommonMethods.LastMilliSecondOfDay(dteDateFrom.Value.AddDays(6)))
                lstMatch.DataBind()
            Else
                'we can't go that far ahead... find the final match week's start date instead
                btnNextWeek.Enabled = False
                btnNextMonth.Enabled = False
                dteDateFrom.Value = MatchManager.FinalMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID)
                'databind the grid
                lstMatch.DataSource = Predictathon.MatchManager.UserMatchPredictionListGet( _
                                                                Predictathon.UserManager.CurrentUserID, _
                                                                Predictathon.CompetitionManager.CurrentCompetitionID, _
                                                                dteDateFrom.Value, _
                                                                CommonMethods.LastMilliSecondOfDay(dteDateFrom.Value.AddDays(6)))
                lstMatch.DataBind()
            End If

            btnPreviousWeek.Enabled = True
            btnPreviousMonth.Enabled = True
        End Sub

        Private Sub btnPreviousWeek_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnPreviousWeek.Click
            'find the previous week's start date
            dteDateFrom.Value = MatchManager.PreviousMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID, dteDateFrom.Value.AddDays(-1))
            If Not dteDateFrom.Value = Date.MinValue Then
                'databind the grid
                lstMatch.DataSource = Predictathon.MatchManager.UserMatchPredictionListGet( _
                                                                Predictathon.UserManager.CurrentUserID, _
                                                                Predictathon.CompetitionManager.CurrentCompetitionID, _
                                                                dteDateFrom.Value, _
                                                                CommonMethods.LastMilliSecondOfDay(dteDateFrom.Value.AddDays(6)))
                lstMatch.DataBind()
            Else
                'we can't go back that far... find the first match week's start date instead
                btnPreviousWeek.Enabled = False
                btnPreviousMonth.Enabled = False
                dteDateFrom.Value = MatchManager.FirstMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID)
                'databind the grid
                lstMatch.DataSource = Predictathon.MatchManager.UserMatchPredictionListGet( _
                                                                Predictathon.UserManager.CurrentUserID, _
                                                                Predictathon.CompetitionManager.CurrentCompetitionID, _
                                                                dteDateFrom.Value, _
                                                                CommonMethods.LastMilliSecondOfDay(dteDateFrom.Value.AddDays(6)))
                lstMatch.DataBind()
            End If

            btnNextWeek.Enabled = True
            btnNextMonth.Enabled = True
        End Sub

        Private Sub btnPreviousMonth_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnPreviousMonth.Click
            'subtract a month from dteDateFrom, then find the previous week's start date
            dteDateFrom.Value = MatchManager.PreviousMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID, dteDateFrom.Value.AddMonths(-1))
            If Not dteDateFrom.Value = Date.MinValue Then
                'databind the grid
                lstMatch.DataSource = Predictathon.MatchManager.UserMatchPredictionListGet( _
                                                                Predictathon.UserManager.CurrentUserID, _
                                                                Predictathon.CompetitionManager.CurrentCompetitionID, _
                                                                dteDateFrom.Value, _
                                                                CommonMethods.LastMilliSecondOfDay(dteDateFrom.Value.AddDays(6)))
                lstMatch.DataBind()
            Else
                'we can't go back that far... find the first match week's start date instead
                btnPreviousWeek.Enabled = False
                btnPreviousMonth.Enabled = False
                dteDateFrom.Value = MatchManager.FirstMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID)
                'databind the grid
                lstMatch.DataSource = Predictathon.MatchManager.UserMatchPredictionListGet(
                                                                Predictathon.UserManager.CurrentUserID,
                                                                Predictathon.CompetitionManager.CurrentCompetitionID,
                                                                dteDateFrom.Value,
                                                                CommonMethods.LastMilliSecondOfDay(dteDateFrom.Value.AddDays(6)))
                lstMatch.DataBind()
            End If

            btnNextWeek.Enabled = True
            btnNextMonth.Enabled = True
        End Sub
    End Class
End Namespace