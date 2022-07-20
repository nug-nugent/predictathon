-- =============================================
-- Author:		David Huggett
-- Create date: 08/10/2011
-- Description:	Returns the league table as it would be for a given competition had every one of a user's predictions come true
-- =============================================

CREATE PROCEDURE [dbo].[CompetitionUserLeagueTableGet]
	@CompetitionID UNIQUEIDENTIFIER
	, @UserID UNIQUEIDENTIFIER
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT
		Team.TeamID
		, Position =  RANK() OVER (ORDER BY (SUM(Results.Won) * 3) + SUM(Results.Drawn) DESC, SUM(Results.GoalsFor - Results.GoalsAgainst) DESC, SUM(Results.GoalsFor) DESC)
		, Played = COUNT(Results.MatchID)
		, Won = SUM(Results.Won)
		, Lost = SUM(Results.Lost)
		, Drawn = SUM(Results.Drawn)
		, Team.ShortName
		, Points = (SUM(Results.Won) * 3) + SUM(Results.Drawn)
		, GoalsFor = SUM(Results.GoalsFor)
		, GoalsAgainst = SUM(Results.GoalsAgainst)
		, GoalDifference = SUM(Results.GoalsFor - Results.GoalsAgainst)
	FROM
		--Home matches
		(SELECT
			Team.TeamID
			, Match.MatchID
			, Won = CASE WHEN Prediction.HomeTeamGoals > Prediction.AwayTeamGoals THEN 1 ELSE 0 END
			, Lost = CASE WHEN Prediction.HomeTeamGoals < Prediction.AwayTeamGoals THEN 1 ELSE 0 END
			, Drawn = CASE WHEN ISNULL(Prediction.HomeTeamGoals, 0) = ISNULL(Prediction.AwayTeamGoals, 0) THEN 1 ELSE 0 END
			, GoalsFor = Prediction.HomeTeamGoals
			, GoalsAgainst = Prediction.AwayTeamGoals
		FROM
			Match
			INNER JOIN Team ON Match.HomeTeamID = Team.TeamID
			LEFT JOIN (SELECT * FROM Prediction WHERE UserID = @UserID) Prediction ON Match.MatchID = Prediction.MatchID
		WHERE
			Match.CompetitionID = @CompetitionID
			AND Match.MatchPlayed = 1
		--Away matches
		UNION
		SELECT
			Team.TeamID
			, Match.MatchID
			, Won = CASE WHEN Prediction.AwayTeamGoals > Prediction.HomeTeamGoals THEN 1 ELSE 0 END
			, Lost = CASE WHEN Prediction.AwayTeamGoals < Prediction.HomeTeamGoals THEN 1 ELSE 0 END
			, Drawn = CASE WHEN ISNULL(Prediction.AwayTeamGoals, 0) = ISNULL(Prediction.HomeTeamGoals, 0) THEN 1 ELSE 0 END
			, GoalsFor = Prediction.AwayTeamGoals
			, GoalsAgainst = Prediction.HomeTeamGoals
		FROM
			Match
			INNER JOIN Team ON Match.AwayTeamID = Team.TeamID
			LEFT JOIN (SELECT * FROM Prediction WHERE UserID = @UserID) Prediction ON Match.MatchID = Prediction.MatchID
		WHERE
			Match.CompetitionID = @CompetitionID
			AND Match.MatchPlayed = 1) Results
		INNER JOIN Team ON Results.TeamID = Team.TeamID
	GROUP BY
		Team.TeamID
		, Team.ShortName
	ORDER BY
		Position
		, Team.ShortName
END
