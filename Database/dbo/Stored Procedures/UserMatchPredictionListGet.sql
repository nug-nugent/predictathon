-- =============================================
-- Author:		David Huggett
-- Create date: 22/9/11
-- Description:	Returns a list of matches with or without predictions by user and date (from/to)
-- =============================================
CREATE PROCEDURE [dbo].[UserMatchPredictionListGet]
	@UserID UNIQUEIDENTIFIER
	, @CompetitionID UNIQUEIDENTIFIER
	, @DateFrom DATETIME = NULL
	, @DateTo DATETIME = NULL
	, @HidePastUnpredictedMatches BIT = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SET @HidePastUnpredictedMatches = ISNULL(@HidePastUnpredictedMatches, 0)

	SELECT
		Match.MatchID
		, Prediction.PredictionID
		, Match.MatchDateTime
		, HomeTeam = ISNULL(HomeTeam.TeamName, Match.HomeTeamTBC)
		, HomeTeamShortName = ISNULL(HomeTeam.ShortName, 'TBC')
		, HomeTeamImage = HomeTeam.ImageName
		, AwayTeam = ISNULL(AwayTeam.TeamName, Match.AwayTeamTBC)
		, AwayTeamShortName = ISNULL(AwayTeam.ShortName, 'TBC')
		, AwayTeamImage = AwayTeam.ImageName
		, Prediction.HomeTeamGoals
		, Prediction.AwayTeamGoals
		, ActualHomeTeamGoals = Match.HomeTeamGoals
		, ActualAwayTeamGoals = Match.AwayTeamGoals
		, Score = CASE WHEN Match.MatchPlayed = 1 AND Prediction.PredictionID IS NULL THEN 0 ELSE Prediction.Score END
		, Match.Description
		, Match.Knockout
	FROM
		Match
		LEFT JOIN Team HomeTeam ON Match.HomeTeamID = HomeTeam.TeamID
		LEFT JOIN Team AwayTeam ON Match.AwayTeamID = AwayTeam.TeamID
		LEFT JOIN (SELECT * FROM Prediction WHERE UserID = @UserID) Prediction ON Match.MatchID = Prediction.MatchID
	WHERE
		Match.CompetitionID = @CompetitionID
		AND (@DateFrom IS NULL OR Match.MatchDateTime >= @DateFrom) 
		AND (@DateTo IS NULL OR Match.MatchDateTime <= @DateTo)
		AND (@HidePastUnpredictedMatches = 0 OR Prediction.PredictionID IS NOT NULL OR Match.MatchDateTime < GETDATE())
	ORDER BY
		Match.MatchDateTime ASC
		, HomeTeam.TeamName
END
