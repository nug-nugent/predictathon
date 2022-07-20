-- =============================================
-- Author:		David Huggett
-- Create date: 27/04/12
-- Description:	Returns a list of the best predictions; ie those where the score is better than average
-- =============================================
CREATE PROCEDURE [dbo].[MatchPredictionAverageBiggestDifferencesGet]
	@CompetitionID UNIQUEIDENTIFIER
	, @DateFrom DATETIME = NULL
	, @DateTo DATETIME = NULL
	, @TeamID UNIQUEIDENTIFIER = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT TOP 50
		Prediction.UserID
		, [User].Username
		, MatchAveragePrediction.*
		, PredictionHomeTeamGoals = Prediction.HomeTeamGoals
		, PredictionAwayTeamGoals = Prediction.AwayTeamGoals
		, PredictionScore = ISNULL(Prediction.Score, 0)
		, ScoreDifference = ISNULL(Prediction.Score, 0) - AveragePredictionScore
	FROM
		(
		SELECT
			Match.MatchID
			, Match.MatchDateTime
			, HomeTeam = ISNULL(HomeTeam.TeamName, Match.HomeTeamTBC)
			, HomeTeamShortName = ISNULL(HomeTeam.ShortName, 'TBC')
			, AwayTeam = ISNULL(AwayTeam.TeamName, Match.AwayTeamTBC)
			, AwayTeamShortName = ISNULL(AwayTeam.ShortName, 'TBC')
			, HomeTeamGoals = Match.HomeTeamGoals
			, AwayTeamGoals = Match.AwayTeamGoals
			, AveragePredictionScore = ISNULL(AVG(CAST(Prediction.Score AS DECIMAL(4, 3))), 0)
		FROM
			Match
			LEFT JOIN Team HomeTeam ON Match.HomeTeamID = HomeTeam.TeamID
			LEFT JOIN Team AwayTeam ON Match.AwayTeamID = AwayTeam.TeamID
			LEFT JOIN Prediction ON Match.MatchID = Prediction.MatchID
		WHERE
			Match.CompetitionID = @CompetitionID
			AND (@DateFrom IS NULL OR Match.MatchDateTime >= @DateFrom) 
			AND (@DateTo IS NULL OR Match.MatchDateTime <= @DateTo)
			AND (@TeamID IS NULL OR Match.HomeTeamID = @TeamID OR Match.AwayTeamID = @TeamID)
			AND Match.MatchPlayed = 1
		GROUP BY
			Match.MatchID
			, Match.MatchDateTime
			, HomeTeam.TeamName
			, Match.HomeTeamTBC
			, HomeTeam.ShortName
			, AwayTeam.TeamName
			, Match.AwayTeamTBC
			, AwayTeam.ShortName
			, Match.HomeTeamGoals
			, Match.AwayTeamGoals
			, Match.Description
			, Match.Knockout) MatchAveragePrediction
		INNER JOIN Prediction ON MatchAveragePrediction.MatchID = Prediction.MatchID
		INNER JOIN [User] ON Prediction.UserID = [User].UserID
	WHERE
		(ISNULL(Prediction.Score, 0) - AveragePredictionScore) > 0
	ORDER BY
		ScoreDifference DESC
		, MatchDateTime DESC
END
