Namespace Predictathon.UserControls.User
    Public Class UserStatistics
        Inherits Predictathon.Web.UI.UserControl

#Region "Properties"
        Private _UserID As Guid
        Public Property UserID() As Guid
            Get
                Return _UserID
            End Get
            Set(ByVal value As Guid)
                _UserID = value
            End Set
        End Property

        Private _Selectable As Boolean = False
        Public Property Selectable() As Boolean
            Get
                Return _Selectable
            End Get
            Set(ByVal value As Boolean)
                _Selectable = value
            End Set
        End Property

        Private _ShowNextPredictionDueIn As Boolean
        Public Property ShowNextPredictionDueIn() As Boolean
            Get
                Return _ShowNextPredictionDueIn
            End Get
            Set(ByVal value As Boolean)
                _ShowNextPredictionDueIn = value
            End Set
        End Property

        Private _ShowUsername As Boolean = True
        Public Property ShowUsername() As Boolean
            Get
                Return _ShowUsername
            End Get
            Set(ByVal value As Boolean)
                _ShowUsername = value
            End Set
        End Property

        Private _NeverShowEditOption As Boolean
        Public Property NeverShowEditOption() As Boolean
            Get
                Return _NeverShowEditOption
            End Get
            Set(ByVal value As Boolean)
                _NeverShowEditOption = value
            End Set
        End Property
#End Region

        Private objUser As PredictathonModel.User

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                objUser = Predictathon.UserManager.Load(UserID)
                lblUsername.Text = If(Me.ShowUsername, objUser.Username, "Statistics")

                If Me.NeverShowEditOption OrElse (Not Me.UserID = Predictathon.UserManager.CurrentUserID AndAlso Not Predictathon.UserManager.CurrentUser.UserAdministrator) Then
                    btnEditUser.Visible = False
                End If

                DisplayUserStatistics()

                If Me.Selectable Then
                    lblUsername.Style.Item("cursor") = "pointer"
                    lblUsername.Attributes("onclick") = Page.ClientScript.GetPostBackEventReference(divUserStatistics, String.Empty, False)
                End If
            ElseIf Request.Form("__EVENTTARGET") = divUserStatistics.UniqueID Then
                'divUserStatistics was clicked
                Response.Redirect("~/Pages/User/UserDetail.aspx?UserID=" & Me.UserID.ToString, False)
            End If
        End Sub

        Protected Sub DisplayUserStatistics()
            'get the user's league position etc for the current competition...
            Dim objUserLeague As PredictathonModel.LeagueTableGet_Result = Predictathon.MatchManager.LeagueTableGet(CompetitionManager.CurrentCompetitionID, Nothing, Nothing, Nothing). _
                Where(Function(Result) Result.UserID = Me.UserID).FirstOrDefault

            If IsNothing(objUserLeague) Then
                'no matches for the current competition (it must have been newly-created)
                lblLeaguePositionValue.Text = "N/A"
                lblPointsValue.Text = "0"
                trLastWeek.Visible = False
                trThisWeek.Visible = False
            Else
                lblLeaguePositionValue.Text = CommonMethods.IntegerToOrdinal(CInt(objUserLeague.LeaguePosition))
                lblPointsValue.Text = CStr(objUserLeague.Score)

                'get the user's league position for last week's matches...
                Dim dteStart As Date = MatchManager.PreviousMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID, MatchManager.MatchWeekStartDate(Date.Today))
                If Not IsNothing(dteStart) AndAlso dteStart <> Date.MinValue Then
                    objUserLeague = Predictathon.MatchManager.LeagueTableGet(CompetitionManager.CurrentCompetitionID, dteStart, dteStart.AddDays(6), Nothing). _
                        Where(Function(Result) Result.UserID = Me.UserID).First
                    lblPointsLastWeekValue.Text = objUserLeague.Score.ToString & " (" & CommonMethods.IntegerToOrdinal(CInt(objUserLeague.LeaguePosition)) & " place)"
                    '..and now this week's games:
                    dteStart = MatchManager.NextMatchWeekEndDateGet(CompetitionManager.CurrentCompetitionID, dteStart.AddDays(7))
                    If Not IsNothing(dteStart) AndAlso dteStart <> Date.MinValue Then
                        dteStart = MatchManager.MatchWeekStartDate(dteStart)
                        'if any matches this week have been processed, show the league placing. Otherwise, forget it...
                        If Predictathon.MatchManager.MatchListGet(CompetitionManager.CurrentCompetitionID, dteStart, Nothing, False).Where(Function(Match) Match.MatchPlayed).Any Then
                            objUserLeague = Predictathon.MatchManager.LeagueTableGet(CompetitionManager.CurrentCompetitionID, dteStart, dteStart.AddDays(6), Nothing). _
                                Where(Function(Result) Result.UserID = Me.UserID).First
                            lblPointsThisWeekValue.Text = objUserLeague.Score.ToString & " (" & CommonMethods.IntegerToOrdinal(CInt(objUserLeague.LeaguePosition)) & " place)"
                        Else
                            lblPointsThisWeekValue.Text = "N/A"
                        End If
                    Else
                        'no games played so far this week: hide the row...
                        trThisWeek.Visible = False
                    End If
                Else
                    trLastWeek.Visible = False
                    trThisWeek.Visible = False
                End If
            End If

            If Me.ShowNextPredictionDueIn Then
                trNextPredictionDueIn.Visible = True
                Dim objNextDuePrediction As PredictathonModel.UserMatchPredictionListGet_Result = MatchManager.UserMatchPredictionListGet(Me.UserID, CompetitionManager.CurrentCompetitionID, Date.Now.AddMinutes(5), Nothing).Where(Function(Result) Result.HomeTeamGoals.HasValue = False).OrderBy(Function(Result) Result.MatchDateTime).FirstOrDefault
                If Not IsNothing(objNextDuePrediction) Then
                    'bear in mind that every match must be predicted at least 5 minutes before kick-off
                    lblNextPredictionDueInValue.Text = CommonMethods.DateDifferenceString(Date.Now, objNextDuePrediction.MatchDateTime.AddMinutes(-5))
                    DateDifferenceToColour(Date.Now, objNextDuePrediction.MatchDateTime, lblNextPredictionDueInValue)
                Else
                    lblNextPredictionDueInValue.Text = "All matches predicted!"
                    lblNextPredictionDueInValue.ForeColor = Drawing.Color.Green
                End If
            Else
                trNextPredictionDueIn.Visible = False
            End If
        End Sub

        Protected Sub DateDifferenceToColour(ByVal FromDate As Date, ByVal ToDate As Date, ByRef lblNextPredictionDueInValue As Label)
            Dim intDays As Long = DateDiff(DateInterval.Day, FromDate, ToDate)
            If intDays >= 4 Then
                lblNextPredictionDueInValue.ForeColor = Drawing.Color.Black
            ElseIf intDays > 0 Then
                lblNextPredictionDueInValue.ForeColor = Drawing.Color.DarkOrange
                lblNextPredictionDueInValue.Font.Bold = True
            ElseIf intDays = 0 Then
                lblNextPredictionDueInValue.ForeColor = Drawing.Color.Red
                lblNextPredictionDueInValue.Font.Bold = True
            End If
        End Sub

        Private Sub btnEditUser_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnEditUser.Click
            Response.Redirect("~/Pages/User/UserDetailEdit.aspx?UserID=" & Me.UserID.ToString, False)
        End Sub
    End Class
End Namespace