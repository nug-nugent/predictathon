-- =============================================
-- Author:		David Huggett
-- Create date: 22/9/11
-- Description:	Returns a list of matches with or without predictions by user and date (from/to)
-- =============================================
CREATE PROCEDURE [dbo].[MatchResultListGet]
	@UserID UNIQUEIDENTIFIER
	, @CompetitionID UNIQUEIDENTIFIER
	, @DateFrom DATETIME = NULL
	, @DateTo DATETIME = NULL
	, @TeamID UNIQUEIDENTIFIER = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT
		m.MatchID
		, m.MatchDateTime
		, HomeTeam = ISNULL(HomeTeam.TeamName, m.HomeTeamTBC)
		, HomeTeamShortName = ISNULL(HomeTeam.ShortName, 'TBC')
		, AwayTeam = ISNULL(AwayTeam.TeamName, m.AwayTeamTBC)
		, AwayTeamShortName = ISNULL(AwayTeam.ShortName, 'TBC')
		, HomeTeamGoals = m.HomeTeamGoals
		, AwayTeamGoals = m.AwayTeamGoals
		, PredictionHomeTeamGoals = YourPrediction.HomeTeamGoals
		, PredictionAwayTeamGoals = YourPrediction.AwayTeamGoals
		, YourPredictionScore = ISNULL(YourPrediction.Score, 0)
		, AveragePredictionScore = ISNULL(AVG(CAST(p.Score AS DECIMAL(4, 3))), CAST(0 AS DECIMAL(4, 3)))
		, m.Description
		, m.Knockout
	FROM
		[dbo].[Match] AS m
		LEFT JOIN [dbo].[Team] AS HomeTeam ON m.HomeTeamID = HomeTeam.TeamID
		LEFT JOIN [dbo].[Team] AS AwayTeam ON m.AwayTeamID = AwayTeam.TeamID
		LEFT JOIN [dbo].[Prediction] AS p ON m.MatchID = p.MatchID
		LEFT JOIN (SELECT p.MatchID, p.Score, p.HomeTeamGoals, p.AwayTeamGoals FROM [dbo].[Prediction] AS p WHERE p.UserID = @UserID) YourPrediction ON m.MatchID = YourPrediction.MatchID
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
		, AwayTeam.TeamName
		, m.AwayTeamTBC
		, AwayTeam.ShortName
		, m.HomeTeamGoals
		, m.AwayTeamGoals
		, YourPrediction.Score
		, YourPrediction.HomeTeamGoals
		, YourPrediction.AwayTeamGoals
		, m.Description
		, m.Knockout
	ORDER BY
		m.MatchDateTime DESC
		, HomeTeam.TeamName
END
