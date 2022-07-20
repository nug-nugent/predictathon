Namespace Predictathon.Pages
    Public Class MainMenu
        Inherits Predictathon.Web.UI.Page

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                Me.Master.TitleSuffix = "Main Menu"

                Dim dteFromDate As Date = MatchManager.PreviousMatchWeekStartDateGet(CompetitionManager.CurrentCompetitionID, (MatchManager.MatchWeekStartDate(Date.Today).AddDays(7)))
                If Not IsNothing(dteFromDate) AndAlso dteFromDate <> Date.MinValue Then
                    UserMatchPredictionList1.DateFrom = dteFromDate.Date 'take time out of the equation
                    UserMatchPredictionList1.DateTo = Predictathon.CommonMethods.LastMilliSecondOfDay(dteFromDate.Date.AddDays(6))
                End If
                If UserMatchPredictionList1.DateTo < Today Then
                    UserMatchPredictionList1.GridTitleText = "Last Matches"
                Else
                    UserMatchPredictionList1.GridTitleText = "Current Matches"
                End If

                dteFromDate = MatchManager.NextMatchWeekEndDateGet(CompetitionManager.CurrentCompetitionID, If(IsNothing(dteFromDate), Date.Today, dteFromDate.AddDays(7)))
                If Not IsNothing(dteFromDate) AndAlso dteFromDate <> Date.MinValue Then
                    UserMatchPredictionList2.DateTo = Predictathon.CommonMethods.LastMilliSecondOfDay(dteFromDate.Date)
                    UserMatchPredictionList2.DateFrom = dteFromDate.Date.AddDays(-6)
                End If
            End If

            UserStatistics1.UserID = Predictathon.UserManager.CurrentUserID
			UserCompetitionRegistrationList1.UserID = Predictathon.UserManager.CurrentUserID
		End Sub

    End Class
End Namespace