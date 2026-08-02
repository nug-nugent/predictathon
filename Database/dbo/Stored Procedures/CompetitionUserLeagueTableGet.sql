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
	SET NOCOUNT ON;

	SELECT
		t.TeamID
		, Position =  RANK() OVER (ORDER BY (SUM(Results.Won) * 3) + SUM(Results.Drawn) DESC, SUM(Results.GoalsFor - Results.GoalsAgainst) DESC, SUM(Results.GoalsFor) DESC)
		, Played = COUNT(Results.MatchID)
		, Won = SUM(Results.Won)
		, Lost = SUM(Results.Lost)
		, Drawn = SUM(Results.Drawn)
		, t.ShortName
		, Points = (SUM(Results.Won) * 3) + SUM(Results.Drawn)
		, GoalsFor = SUM(Results.GoalsFor)
		, GoalsAgainst = SUM(Results.GoalsAgainst)
		, GoalDifference = SUM(Results.GoalsFor - Results.GoalsAgainst)
	FROM
		--Home matches
		(SELECT
			t.TeamID
			, m.MatchID
			, Won = CASE WHEN Prediction.HomeTeamGoals > Prediction.AwayTeamGoals THEN 1 ELSE 0 END
			, Lost = CASE WHEN Prediction.HomeTeamGoals < Prediction.AwayTeamGoals THEN 1 ELSE 0 END
			, Drawn = CASE WHEN ISNULL(Prediction.HomeTeamGoals, 0) = ISNULL(Prediction.AwayTeamGoals, 0) THEN 1 ELSE 0 END
			, GoalsFor = Prediction.HomeTeamGoals
			, GoalsAgainst = Prediction.AwayTeamGoals
		FROM
			[dbo].[Match] AS m
			INNER JOIN [dbo].[Team] AS t ON m.HomeTeamID = t.TeamID
			LEFT JOIN (SELECT p.PredictionID, p.MatchID, p.UserID, p.HomeTeamGoals, p.AwayTeamGoals, p.Score, p.GoalDifference, p.PredictionHistoryID, p.Invalid, p.AutoUpdatedDueToLatePrediction FROM [dbo].[Prediction] AS p WHERE p.UserID = @UserID) Prediction ON m.MatchID = Prediction.MatchID
		WHERE
			m.CompetitionID = @CompetitionID
			AND m.MatchPlayed = 1
		--Away matches
		UNION
		SELECT
			t.TeamID
			, m.MatchID
			, Won = CASE WHEN Prediction.AwayTeamGoals > Prediction.HomeTeamGoals THEN 1 ELSE 0 END
			, Lost = CASE WHEN Prediction.AwayTeamGoals < Prediction.HomeTeamGoals THEN 1 ELSE 0 END
			, Drawn = CASE WHEN ISNULL(Prediction.AwayTeamGoals, 0) = ISNULL(Prediction.HomeTeamGoals, 0) THEN 1 ELSE 0 END
			, GoalsFor = Prediction.AwayTeamGoals
			, GoalsAgainst = Prediction.HomeTeamGoals
		FROM
			[dbo].[Match] AS m
			INNER JOIN [dbo].[Team] AS t ON m.AwayTeamID = t.TeamID
			LEFT JOIN (SELECT p.PredictionID, p.MatchID, p.UserID, p.HomeTeamGoals, p.AwayTeamGoals, p.Score, p.GoalDifference, p.PredictionHistoryID, p.Invalid, p.AutoUpdatedDueToLatePrediction FROM [dbo].[Prediction] AS p WHERE p.UserID = @UserID) Prediction ON m.MatchID = Prediction.MatchID
		WHERE
			m.CompetitionID = @CompetitionID
			AND m.MatchPlayed = 1) Results
		INNER JOIN [dbo].[Team] AS t ON Results.TeamID = t.TeamID
	GROUP BY
		t.TeamID
		, t.ShortName
	ORDER BY
		Position
		, t.ShortName;
END;