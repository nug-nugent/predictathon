Namespace Predictathon
    Public Class MatchManager
        Inherits Predictathon.Manager
        Public Const EntitySetName As String = "Matches"

        Public Shared Function Load(ByVal MatchID As Guid) As PredictathonModel.Match
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Matches.FirstOrDefault(Function(Match) Match.MatchID = MatchID)
            End Using
        End Function

        Public Shared Sub Delete(ByVal Match As PredictathonModel.Match)
            Predictathon.Data.Delete(Match, EntitySetName)
        End Sub

        Public Shared Sub Save(ByVal Match As PredictathonModel.Match)
            Predictathon.Data.Save(Match, EntitySetName)
            MatchPredictionScoreSet(Match.MatchID)
        End Sub

        Public Shared Function Create(ByVal MatchID As Guid, ByVal CompetitionID As Guid, ByVal HomeTeamID As Guid?, ByVal AwayTeamID As Guid?) As PredictathonModel.Match
            Return New PredictathonModel.Match With {.MatchID = MatchID, .CompetitionID = CompetitionID, .HomeTeamID = HomeTeamID, .AwayTeamID = AwayTeamID}
        End Function

        Public Shared Function MatchPredictionListGet(ByVal MatchID As Guid) As List(Of PredictathonModel.MatchPredictionListGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.MatchPredictionListGet(MatchID).ToList
            End Using
        End Function

        Public Shared Function UserMatchPredictionListGet(ByVal UserID As Guid, ByVal CompetitionID As Guid, ByVal DateFrom As Date?, ByVal DateTo As Date?, Optional ByVal HidePastUnpredictedMatches As Boolean = False) As List(Of PredictathonModel.UserMatchPredictionListGet_Result)
            If DateFrom = Date.MinValue Then DateFrom = Nothing
            If DateTo = Date.MinValue Then DateTo = Nothing
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.UserMatchPredictionListGet(UserID, CompetitionID, DateFrom, DateTo, HidePastUnpredictedMatches).ToList
            End Using
        End Function

        ''' <summary>
        ''' Returns a list of completed matches
        ''' </summary>
        ''' <param name="CompetitionID"></param>
        ''' <param name="DateFrom"></param>
        ''' <param name="DateTo"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function MatchResultListGet(ByVal UserID As Guid, ByVal CompetitionID As Guid, ByVal DateFrom As Date?, ByVal DateTo As Date?, ByVal TeamID As Guid?) As List(Of PredictathonModel.MatchResultListGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.MatchResultListGet(UserID, CompetitionID, DateFrom, DateTo, TeamID).ToList
            End Using
        End Function

        Public Shared Function MatchListGet(ByVal CompetitionID As Guid, ByVal DateFrom As Date?, ByVal DateTo As Date?, ByVal UnprocessedOnly As Boolean) As List(Of PredictathonModel.MatchListGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.MatchListGet(CompetitionID, DateFrom, DateTo, UnprocessedOnly).ToList
            End Using
        End Function

        Public Shared Function MatchListGet(ByVal CompetitionID As Guid, ByVal DateFrom As Date?, ByVal DateTo As Date?, ByVal HomeTeamID As Guid?, ByVal AwayTeamID As Guid?, ByVal OrderByDateDescending As Boolean) As List(Of PredictathonModel.Match)
            Using objContext As New PredictathonModel.PredictathonEntities
                If OrderByDateDescending Then
                    Return objContext.Matches.OrderByDescending(Of DateTime)(Function(Match) Match.MatchDateTime).Where _
                    (Function(Match) _
                         Match.CompetitionID = CompetitionID _
                         AndAlso (DateFrom.HasValue = False OrElse Match.MatchDateTime >= DateFrom.Value) _
                         AndAlso (DateTo.HasValue = False OrElse Match.MatchDateTime <= DateTo.Value) _
                         AndAlso (HomeTeamID.HasValue = False OrElse Match.HomeTeamID.Value = HomeTeamID.Value) _
                         AndAlso (AwayTeamID.HasValue = False OrElse Match.AwayTeamID.Value = AwayTeamID.Value) _
                    ).ToList
                Else
                    Return objContext.Matches.OrderBy(Of DateTime)(Function(Match) Match.MatchDateTime).Where _
                    (Function(Match) _
                         Match.CompetitionID = CompetitionID _
                         AndAlso (DateFrom.HasValue = False OrElse Match.MatchDateTime >= DateFrom.Value) _
                         AndAlso (DateTo.HasValue = False OrElse Match.MatchDateTime <= DateTo.Value) _
                         AndAlso (HomeTeamID.HasValue = False OrElse Match.HomeTeamID.Value = HomeTeamID.Value) _
                         AndAlso (AwayTeamID.HasValue = False OrElse Match.AwayTeamID.Value = AwayTeamID.Value) _
                    ).ToList
                End If
            End Using
        End Function

        Public Shared Function MatchTeamResultListGet(ByVal TeamID As Guid) As List(Of PredictathonModel.Match)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Matches.Where _
                    (Function(Match) _
                         (Match.HomeTeamID.Value = TeamID _
                         OrElse Match.AwayTeamID.Value = TeamID) _
                         AndAlso Match.MatchPlayed).ToList
            End Using
        End Function

        ''' <summary>
        ''' Updates the score for all predictions for a given match
        ''' </summary>
        ''' <param name="MatchID"></param>
        ''' <remarks></remarks>
        Protected Shared Sub MatchPredictionScoreSet(ByVal MatchID As Guid)
            Using objContext As New PredictathonModel.PredictathonEntities
                objContext.MatchPredictionScoreSet(MatchID)
            End Using
        End Sub

        Public Shared Function LeagueTableGet(ByVal CompetitionID As Guid, ByVal DateFrom As Date?, ByVal DateTo As Date?, ByVal DateForComparison As Date?) As List(Of PredictathonModel.LeagueTableGet_Result)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.LeagueTableGet(CompetitionID, If(DateFrom = Date.MinValue, Nothing, DateFrom), If(DateTo = Date.MinValue, Nothing, DateTo), If(DateForComparison = Date.MinValue, Nothing, DateForComparison)).ToList
            End Using
        End Function

        ''' <summary>
        ''' Returns what should be the start of the current match week. If, however, there are no matches in the week, this figure will be incorrect.
        ''' Use in conjunction with LastMatchWeekStartDateGet to get the correct figure.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function MatchWeekStartDate(ByVal MatchDate As Date) As Date
            Return CommonMethods.NextDayOfWeekDate(MatchDate, DayOfWeek.Friday).AddDays(-7)
        End Function

        ''' <summary>
        ''' All 'match weeks' begin on a Friday. Given any date, this will return the previous Friday 
        ''' on which any of the following 6 days feature a match.
        ''' </summary>
        ''' <param name="CompetitionID"></param>
        ''' <param name="DateTo"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function PreviousMatchWeekStartDateGet(ByVal CompetitionID As Guid, ByVal DateTo As Date, Optional ByVal CompletedMatchesOnly As Boolean = False) As Date
            'When did the last match (before DateTo) occur?
            Using objContext As New PredictathonModel.PredictathonEntities
                Dim dteLastMatchDate As Date = (From Match In objContext.Matches Where
                                             Match.CompetitionID = CompetitionID AndAlso Match.MatchDateTime <= DateTo _
                                             AndAlso (CompletedMatchesOnly = False OrElse Match.MatchPlayed = True)
                                             Order By Match.MatchDateTime Descending
                                             Select Match.MatchDateTime).FirstOrDefault
                If Not IsNothing(dteLastMatchDate) AndAlso dteLastMatchDate <> Date.MinValue Then
                    'what's the Friday before LastMatchDate?
                    Return MatchWeekStartDate(dteLastMatchDate)
                Else
                    Return Nothing
                End If
            End Using
        End Function

        ''' <summary>
        ''' All 'match weeks' end on a Thursday. Given any date, this will return the next Thursday
        ''' on which any of the previous 6 days featured a match.
        ''' </summary>
        ''' <param name="CompetitionID"></param>
        ''' <param name="DateFrom"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function NextMatchWeekEndDateGet(ByVal CompetitionID As Guid, ByVal DateFrom As Date, Optional ByVal CompletedMatchesOnly As Boolean = False) As Date
            'Firstly ensure that time of day doesn't make any odds
            DateFrom = CommonMethods.LastMilliSecondOfDay(DateFrom)

            'Now, when does the next match (after DateFrom) occur?
            Using objContext As New PredictathonModel.PredictathonEntities
                Dim dteNextMatchDate As Date = (From Match In objContext.Matches Where
                                             Match.CompetitionID = CompetitionID AndAlso Match.MatchDateTime >= DateFrom _
                                             AndAlso (CompletedMatchesOnly = False OrElse Match.MatchPlayed = True)
                                             Order By Match.MatchDateTime Ascending
                                             Select Match.MatchDateTime).FirstOrDefault
                If Not IsNothing(dteNextMatchDate) AndAlso dteNextMatchDate <> Date.MinValue Then
                    'what's the Thursday after LastMatchDate?
                    Return MatchWeekStartDate(dteNextMatchDate).AddDays(6)
                Else
                    Return Nothing
                End If
            End Using
        End Function

        Protected Shared Function FirstMatchDateGet(ByVal CompetitionID As Guid) As Date
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Matches.Where(Function(Match) Match.CompetitionID = CompetitionID).OrderBy(Function(Match) Match.MatchDateTime).FirstOrDefault.MatchDateTime
            End Using
        End Function

        Public Shared Function FirstMatchWeekStartDateGet(ByVal CompetitionID As Guid) As Date
            Return MatchWeekStartDate(FirstMatchDateGet(CompetitionID))
        End Function

        Public Shared Function FinalMatchWeekStartDateGet(ByVal CompetitionID As Guid, Optional ByVal CompletedMatchesOnly As Boolean = False) As Date
            Return MatchWeekStartDate(LastMatchDateGet(CompetitionID, CompletedMatchesOnly))
        End Function

        Protected Shared Function LastMatchDateGet(ByVal CompetitionID As Guid, Optional ByVal CompletedMatchesOnly As Boolean = False) As Date
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Matches.Where(Function(Match) Match.CompetitionID = CompetitionID AndAlso (CompletedMatchesOnly = False OrElse Match.MatchPlayed = True)).OrderByDescending(Function(Match) Match.MatchDateTime).FirstOrDefault.MatchDateTime
            End Using
        End Function
    End Class
End Namespace
