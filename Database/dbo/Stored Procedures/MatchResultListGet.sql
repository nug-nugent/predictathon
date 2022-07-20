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
		Match.MatchID
		, Match.MatchDateTime
		, HomeTeam = ISNULL(HomeTeam.TeamName, Match.HomeTeamTBC)
		, HomeTeamShortName = ISNULL(HomeTeam.ShortName, 'TBC')
		, AwayTeam = ISNULL(AwayTeam.TeamName, Match.AwayTeamTBC)
		, AwayTeamShortName = ISNULL(AwayTeam.ShortName, 'TBC')
		, HomeTeamGoals = Match.HomeTeamGoals
		, AwayTeamGoals = Match.AwayTeamGoals
		, PredictionHomeTeamGoals = YourPrediction.HomeTeamGoals
		, PredictionAwayTeamGoals = YourPrediction.AwayTeamGoals
		, YourPredictionScore = ISNULL(YourPrediction.Score, 0)
		, AveragePredictionScore = ISNULL(AVG(CAST(Prediction.Score AS DECIMAL(4, 3))), 0)
		, Match.Description
		, Match.Knockout
	FROM
		Match
		LEFT JOIN Team HomeTeam ON Match.HomeTeamID = HomeTeam.TeamID
		LEFT JOIN Team AwayTeam ON Match.AwayTeamID = AwayTeam.TeamID
		LEFT JOIN Prediction ON Match.MatchID = Prediction.MatchID
		LEFT JOIN (SELECT MatchID, Score, HomeTeamGoals, AwayTeamGoals FROM Prediction WHERE UserID = @UserID) YourPrediction ON Match.MatchID = YourPrediction.MatchID
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
		, YourPrediction.Score
		, YourPrediction.HomeTeamGoals
		, YourPrediction.AwayTeamGoals
		, Match.Description
		, Match.Knockout
	ORDER BY
		Match.MatchDateTime DESC
		, HomeTeam.TeamName
END
