Namespace Predictathon.Pages.Team
    Partial Public Class TeamDetail
        Inherits Predictathon.Web.UI.Page

#Region "Properties"
        Private ReadOnly Property TeamID As Guid
            Get
                Return New Guid(Request.QueryString("TeamID"))
            End Get
        End Property
#End Region

        Private Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init
            MatchResultList1.TeamID = Me.TeamID
        End Sub

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                Dim objTeam As PredictathonModel.Team = Predictathon.TeamManager.Load(Me.TeamID)
                Me.Master.TitleSuffix = "Team Detail - " & objTeam.TeamName
                imgTeam.ImageUrl = If(String.IsNullOrEmpty(objTeam.ImageName), "~/Images/Common/spacer.gif", "~/Images/TeamCrests/" & objTeam.ImageName).ToString
                lblTeamName.Text = objTeam.TeamName

                TeamStatisticsGet()
            End If
        End Sub

        Protected Sub TeamStatisticsGet()
            Dim lstResults As List(Of PredictathonModel.Match) = Predictathon.MatchManager.MatchTeamResultListGet(Me.TeamID)
			Dim intTotalMatches As Integer = lstResults.Where(Function(Match) Match.CompetitionID = CompetitionManager.CurrentCompetitionID).Count
            Dim intHomeMatches As Integer = lstResults.Where(Function(Match) Match.CompetitionID = CompetitionManager.CurrentCompetitionID AndAlso Match.HomeTeamID.Value = Me.TeamID AndAlso Not Match.NeutralGround).Count
            Dim intAwayMatches As Integer = lstResults.Where(Function(Match) Match.CompetitionID = CompetitionManager.CurrentCompetitionID AndAlso Match.AwayTeamID.Value = Me.TeamID AndAlso Not Match.NeutralGround).Count
            Dim intNeutralGoalsScored As Integer = lstResults.Where(Function(Match) Match.CompetitionID = CompetitionManager.CurrentCompetitionID AndAlso Match.CompetitionID = CompetitionManager.CurrentCompetitionID AndAlso Match.HomeTeamID.Value = Me.TeamID AndAlso Match.NeutralGround).Sum(Function(Match) Match.HomeTeamGoals.Value) + _
                                                   lstResults.Where(Function(Match) Match.CompetitionID = CompetitionManager.CurrentCompetitionID AndAlso Match.AwayTeamID.Value = Me.TeamID AndAlso Match.NeutralGround).Sum(Function(Match) Match.AwayTeamGoals.Value)
            Dim intNeutralGoalsConceded As Integer = lstResults.Where(Function(Match) Match.CompetitionID = CompetitionManager.CurrentCompetitionID AndAlso Match.HomeTeamID.Value = Me.TeamID AndAlso Match.NeutralGround).Sum(Function(Match) Match.AwayTeamGoals.Value) + _
                                                   lstResults.Where(Function(Match) Match.CompetitionID = CompetitionManager.CurrentCompetitionID AndAlso Match.AwayTeamID.Value = Me.TeamID AndAlso Match.NeutralGround).Sum(Function(Match) Match.HomeTeamGoals.Value)
            Dim intHomeGoalsScored As Integer? = lstResults.Where(Function(Match) Match.CompetitionID = CompetitionManager.CurrentCompetitionID AndAlso Match.HomeTeamID.Value = Me.TeamID AndAlso Not Match.NeutralGround).Sum(Function(Match) Match.HomeTeamGoals.Value)
            Dim intHomeGoalsConceded As Integer? = lstResults.Where(Function(Match) Match.CompetitionID = CompetitionManager.CurrentCompetitionID AndAlso Match.HomeTeamID.Value = Me.TeamID AndAlso Not Match.NeutralGround).Sum(Function(Match) Match.AwayTeamGoals.Value)
            Dim intAwayGoalsScored As Integer? = lstResults.Where(Function(Match) Match.CompetitionID = CompetitionManager.CurrentCompetitionID AndAlso Match.AwayTeamID.Value = Me.TeamID AndAlso Not Match.NeutralGround).Sum(Function(Match) Match.AwayTeamGoals.Value)
            Dim intAwayGoalsConceded As Integer? = lstResults.Where(Function(Match) Match.CompetitionID = CompetitionManager.CurrentCompetitionID AndAlso Match.AwayTeamID.Value = Me.TeamID AndAlso Not Match.NeutralGround).Sum(Function(Match) Match.HomeTeamGoals.Value)

            If intHomeMatches > 0 Then
                lblAverageGoalsForHomeValue.Text = (intHomeGoalsScored.Value / intHomeMatches).ToString("N2")
                lblAverageGoalsAgainstHomeValue.Text = (intHomeGoalsConceded.Value / intHomeMatches).ToString("N2")
            Else
                trAverageGoalsForHome.Visible = False
                trAverageGoalsAgainstHome.Visible = False
            End If

            If intAwayMatches > 0 Then
                lblAverageGoalsForAwayValue.Text = (intAwayGoalsScored.Value / intAwayMatches).ToString("N2")
                lblAverageGoalsAgainstAwayValue.Text = (intAwayGoalsConceded.Value / intAwayMatches).ToString("N2")
            Else
                trAverageGoalsForAway.Visible = False
                trAverageGoalsAgainstAway.Visible = False
            End If

            If intTotalMatches > 0 Then
                lblAverageGoalsForTotalValue.Text = ((If(intHomeGoalsScored, 0) + If(intAwayGoalsScored, 0) + intNeutralGoalsScored) / intTotalMatches).ToString("N2")
                lblAverageGoalsAgainstTotalValue.Text = ((If(intHomeGoalsConceded, 0) + If(intAwayGoalsConceded, 0) + intNeutralGoalsConceded) / intTotalMatches).ToString("N2")
            Else
                trAverageGoalsForTotal.Visible = False
                trAverageGoalsAgainstTotal.Visible = False
            End If

            lblGoalsFor.Text = "Goals for: " & (If(intHomeGoalsScored, 0) + If(intAwayGoalsScored, 0) + intNeutralGoalsScored).ToString
            lblGoalsAgainst.Text = "Goals against: " & (If(intHomeGoalsConceded, 0) + If(intAwayGoalsConceded, 0) + intNeutralGoalsConceded).ToString
        End Sub
    End Class
End Namespace