Namespace Predictathon.Pages.Match
    Partial Public Class Predictions
        Inherits Web.UI.Page

        Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "Predictions"

            If Not IsPostBack AndAlso CheckForCallBack() = False Then
                ' Set the CompetitionWeeksJson, LoadedWeek and MatchesJson properties accordingly to be used by the React app...

                ' Get the list of weeks for this competition
                Dim competitionWeeks = CompetitionManager.GetCompetitionWeeks(CompetitionManager.CurrentCompetitionID)
                Dim weekStrings = competitionWeeks.Select(Function(d) d.ToString("s"))
                CompetitionWeeksJson = (New Script.Serialization.JavaScriptSerializer).Serialize(weekStrings)

                ' Show current week (that has matches), or competition's first week if we're early
                LoadedWeek = MatchManager.MatchWeekStartDate(Date.Today)
                Dim nextWeek = competitionWeeks.FirstOrDefault(Function(week) week > LoadedWeek)
                If nextWeek = Date.MinValue Then
                    ' after the competition
                    LoadedWeek = competitionWeeks.Last()
                ElseIf nextWeek = competitionWeeks.First() Then
                    ' before the competition
                    LoadedWeek = nextWeek
                Else
                    ' in the middle somewhere
                    LoadedWeek = competitionWeeks.ElementAt(competitionWeeks.IndexOf(nextWeek) - 1)
                End If

                ' Get the matches for the week
                MatchesJson = GetMatchesJson(LoadedWeek)
            End If
        End Sub

        Public LoadedWeek As Date
        Public CompetitionWeeksJson As String
        Public MatchesJson As String

        Public Function CheckForCallBack() As Boolean
            Dim strCallBack As String = Request.Params("CallBack")
            If String.IsNullOrEmpty(strCallBack) Then
                Return False
            ElseIf strCallBack = "GetMatches" Then
                GetMatchesFromCallback()
            ElseIf strCallBack = "SavePrediction" Then
                SavePredictionFromCallBack()
            ElseIf strCallBack = "GetPredictions" Then
                GetPredictions()
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

                Predictathon.PredictionManager.CreateAndSave(MatchID,
                                                             PredictionID,
                                                             Predictathon.UserManager.CurrentUserID,
                                                             HomeTeamGoals,
                                                             AwayTeamGoals)
                Return True
            Else
                Return False 'too late!
            End If
        End Function

        Private Sub GetPredictions()
            Dim matchId As Guid
            Try
                Guid.TryParse(Request.Params("MatchID"), matchId)
                If matchId = Guid.Empty Then
                    Throw New Exception()
                End If

                Dim match = MatchManager.Load(matchId)
                If match.MatchDateTime > Date.Now.AddMinutes(5) Then
                    Throw New Exception()
                End If
            Catch ex As Exception
                Response.StatusCode = 400
                Response.End()
                Return
            End Try

            Dim predictions = MatchManager.MatchPredictionListGet(matchId).AsEnumerable().Select(Function(p) New With {
                .username = p.Username,
                .isMe = UserManager.CurrentUserID = p.UserID,
                .homeGoals = p.HomeTeamGoals,
                .awayGoals = p.AwayTeamGoals,
                .points = p.Score
            })

            Dim objSerializer = New Script.Serialization.JavaScriptSerializer
            Response.Write(objSerializer.Serialize(predictions))
            Response.End()
        End Sub

        Private Sub GetMatchesFromCallback()
            Dim dateFrom As Date

            If Date.TryParse(Request.Params("DateFrom"), dateFrom) Then
                Response.Write(GetMatchesJson(dateFrom))
            Else
                Response.StatusCode = 400
            End If

            Response.End()
        End Sub

        Private Function GetMatchesJson(dateFrom As Date) As String
            Dim dateTo = dateFrom.AddDays(7).AddMilliseconds(-1)

            Dim matches = MatchManager.UserMatchPredictionListGet(UserManager.CurrentUserID,
                                                                  CompetitionManager.CurrentCompetitionID,
                                                                  dateFrom, dateTo)

            Dim mapped = matches.AsEnumerable().Select(Function(m) New With {
                .id = m.MatchID,
                .date = m.MatchDateTime.ToString("s"),
                .timeZone = "Europe/London", ' we assume all dates are UK time
                .description = m.Description,
                .isKnockout = m.Knockout,
                .homeTeam = m.HomeTeam,
                .homeImage = m.HomeTeamImage,
                .awayTeam = m.AwayTeam,
                .awayImage = m.AwayTeamImage,
                .predictedHomeGoals = If(m.ActualHomeTeamGoals.HasValue And m.Score.HasValue = False, Nothing, m.HomeTeamGoals),
                .predictedAwayGoals = If(m.ActualHomeTeamGoals.HasValue And m.Score.HasValue = False, Nothing, m.AwayTeamGoals),
                .actualHomeGoals = m.ActualHomeTeamGoals,
                .actualAwayGoals = m.ActualAwayTeamGoals,
                .points = m.Score
            })

            Return (New Script.Serialization.JavaScriptSerializer).Serialize(mapped)
        End Function
    End Class
End Namespace