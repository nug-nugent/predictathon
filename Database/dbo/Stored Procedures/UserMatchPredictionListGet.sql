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
	SET NOCOUNT ON;

	SET @HidePastUnpredictedMatches = ISNULL(@HidePastUnpredictedMatches, 0);

	SELECT
		m.MatchID
		, Prediction.PredictionID
		, m.MatchDateTime
		, m.HomeTeamID
		, HomeTeam = ISNULL(HomeTeam.TeamName, m.HomeTeamTBC)
		, HomeTeamShortName = ISNULL(HomeTeam.ShortName, 'TBC')
		, HomeTeamImage = HomeTeam.ImageName
		, m.AwayTeamID
		, AwayTeam = ISNULL(AwayTeam.TeamName, m.AwayTeamTBC)
		, AwayTeamShortName = ISNULL(AwayTeam.ShortName, 'TBC')
		, AwayTeamImage = AwayTeam.ImageName
		, Prediction.HomeTeamGoals
		, Prediction.AwayTeamGoals
		, ActualHomeTeamGoals = m.HomeTeamGoals
		, ActualAwayTeamGoals = m.AwayTeamGoals
		, m.MatchPlayed
		-- The provisional in-play score, NULL until something has been heard about the match.
		-- Kept firmly apart from ActualHome/AwayTeamGoals above, which only ever hold a
		-- confirmed, scored result - see dbo.MatchLiveScore.
		, LiveHomeTeamGoals = LiveScore.HomeTeamGoals
		, LiveAwayTeamGoals = LiveScore.AwayTeamGoals
		-- When the scoreline itself last moved, and when we last heard from the provider at all.
		-- The reader is shown the latter: on a quiet spell the former stops moving and starts
		-- reading as a stall.
		, LiveScoreUpdatedDateTime = LiveScore.UpdatedDateTime
		, LiveScoreLastPolledDateTime = LiveScore.LastPolledDateTime
		, Score = CASE WHEN m.MatchPlayed = 1 AND Prediction.PredictionID IS NULL THEN 0 ELSE Prediction.Score END
		, m.Description
		, m.Knockout
	FROM
		[dbo].[Match] AS m
		LEFT JOIN [dbo].[Team] AS HomeTeam ON m.HomeTeamID = HomeTeam.TeamID
		LEFT JOIN [dbo].[Team] AS AwayTeam ON m.AwayTeamID = AwayTeam.TeamID
		LEFT JOIN [dbo].[MatchLiveScore] AS LiveScore ON LiveScore.MatchID = m.MatchID
		LEFT JOIN (SELECT p.PredictionID, p.MatchID, p.UserID, p.HomeTeamGoals, p.AwayTeamGoals, p.Score, p.GoalDifference, p.PredictionHistoryID, p.Invalid, p.AutoUpdatedDueToLatePrediction FROM [dbo].[Prediction] AS p WHERE p.UserID = @UserID) Prediction ON m.MatchID = Prediction.MatchID
	WHERE
		m.CompetitionID = @CompetitionID
		AND (@DateFrom IS NULL OR m.MatchDateTime >= @DateFrom)
		AND (@DateTo IS NULL OR m.MatchDateTime <= @DateTo)
		AND (@HidePastUnpredictedMatches = 0 OR Prediction.PredictionID IS NOT NULL OR m.MatchDateTime < GETDATE())
	ORDER BY
		m.MatchDateTime ASC
		, HomeTeam.TeamName;
END;