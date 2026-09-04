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
	SET NOCOUNT ON;

	SELECT TOP (50)
		p.UserID
		, [User].UserName AS Username
		, MatchAveragePrediction.MatchID
		, MatchAveragePrediction.MatchDateTime
		, MatchAveragePrediction.HomeTeam
		, MatchAveragePrediction.HomeTeamShortName
		, MatchAveragePrediction.HomeTeamAcronym
		, MatchAveragePrediction.AwayTeam
		, MatchAveragePrediction.AwayTeamShortName
		, MatchAveragePrediction.AwayTeamAcronym
		, MatchAveragePrediction.HomeTeamGoals
		, MatchAveragePrediction.AwayTeamGoals
		, MatchAveragePrediction.AveragePredictionScore
		, PredictionHomeTeamGoals = p.HomeTeamGoals
		, PredictionAwayTeamGoals = p.AwayTeamGoals
		, PredictionScore = ISNULL(p.Score, CAST(0 AS INT))
		, ScoreDifference = ISNULL(p.Score, CAST(0 AS INT)) - AveragePredictionScore
	FROM
		(
		SELECT
			m.MatchID
			, m.MatchDateTime
			, HomeTeam = ISNULL(HomeTeam.TeamName, m.HomeTeamTBC)
			, HomeTeamShortName = ISNULL(HomeTeam.ShortName, 'TBC')
			, HomeTeamAcronym = HomeTeam.Acronym
			, AwayTeam = ISNULL(AwayTeam.TeamName, m.AwayTeamTBC)
			, AwayTeamShortName = ISNULL(AwayTeam.ShortName, 'TBC')
			, AwayTeamAcronym = AwayTeam.Acronym
			, HomeTeamGoals = m.HomeTeamGoals
			, AwayTeamGoals = m.AwayTeamGoals
			, AveragePredictionScore = ISNULL(AVG(CAST(p.Score AS DECIMAL(4, 3))), CAST(0 AS DECIMAL(4, 3)))
		FROM
			[dbo].[Match] AS m
			LEFT JOIN [dbo].[Team] AS HomeTeam ON m.HomeTeamID = HomeTeam.TeamID
			LEFT JOIN [dbo].[Team] AS AwayTeam ON m.AwayTeamID = AwayTeam.TeamID
			LEFT JOIN [dbo].[Prediction] AS p ON m.MatchID = p.MatchID
		WHERE
			m.CompetitionID = @CompetitionID
			AND (@DateFrom IS NULL OR m.MatchDateTime >= @DateFrom)
			AND (@DateTo IS NULL OR m.MatchDateTime <= @DateTo)
			AND (@TeamID IS NULL OR m.HomeTeamID = @TeamID OR m.AwayTeamID = @TeamID)
			AND m.MatchPlayed = 1
		GROUP BY
			m.MatchID
			, m.MatchDateTime
			, HomeTeam.TeamName
			, m.HomeTeamTBC
			, HomeTeam.ShortName
			, HomeTeam.Acronym
			, AwayTeam.TeamName
			, m.AwayTeamTBC
			, AwayTeam.ShortName
			, AwayTeam.Acronym
			, m.HomeTeamGoals
			, m.AwayTeamGoals
			, m.Description
			, m.Knockout) MatchAveragePrediction
		INNER JOIN [dbo].[Prediction] AS p ON MatchAveragePrediction.MatchID = p.MatchID
		INNER JOIN [Identity].[Users] AS [User] ON p.UserID = [User].Id
	WHERE
		(ISNULL(p.Score, CAST(0 AS INT)) - AveragePredictionScore) > 0
	ORDER BY
		ScoreDifference DESC
		, MatchAveragePrediction.MatchDateTime DESC;
END;