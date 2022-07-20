/* ==============================================================================================================
	Description
		Updates the score for all predictions for a given match

	Update History
		22/09/2011 - DH - Created
		01/07/2019 - DH - Removed 2-pointers where competition settings dictate
		01/06/2022 - DH - Added update from latest valid PredictionHistory section
============================================================================================================== */
CREATE PROCEDURE [dbo].[MatchPredictionScoreSet]
	@MatchID UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

	-- Ensure that no predictions were made later than the current match date/time - if they were, attempt to make them valid by
	-- reverting to the user's last valid prediction.
	UPDATE
		Prediction
	SET
		HomeTeamGoals = ISNULL(LatestValidPredictionHistory.HomeTeamGoals, Prediction.HomeTeamGoals)
		, AwayTeamGoals = ISNULL(LatestValidPredictionHistory.AwayTeamGoals, Prediction.AwayTeamGoals)
		, AutoUpdatedDueToLatePrediction = CASE WHEN LatestValidPredictionHistory.PredictionHistoryID IS NULL THEN 0 ELSE 1 END
		, PredictionHistoryID = ISNULL(LatestValidPredictionHistory.PredictionHistoryID, Prediction.PredictionHistoryID)
		, Invalid = CASE WHEN LatestValidPredictionHistory.PredictionHistoryID IS NULL THEN 1 ELSE 0 END
	FROM
		Prediction
		INNER JOIN PredictionHistory CurrentPredictionHistory ON Prediction.PredictionHistoryID = CurrentPredictionHistory.PredictionHistoryID
		INNER JOIN Match ON Prediction.MatchID = Match.MatchID
		LEFT JOIN 
		(
			SELECT
				Prediction.UserID
				, Prediction.PredictionID
				, LatestValidPredictionHistoryID = MAX(PredictionHistory.PredictionHistoryID)
			FROM
				PredictionHistory
				INNER JOIN Prediction ON PredictionHistory.PredictionID = Prediction.PredictionID
				INNER JOIN Match ON Prediction.MatchID = Match.MatchID
			WHERE
				Prediction.MatchID = @MatchID
				AND PredictionHistory.PredictionDateTime <= Match.MatchDateTime
			GROUP BY
				Prediction.UserID
				, Prediction.PredictionID
		) LatestValidPredictionHistoryByUser ON 
			Prediction.PredictionID = LatestValidPredictionHistoryByUser.PredictionID
			AND Prediction.UserID = LatestValidPredictionHistoryByUser.UserID
		LEFT JOIN PredictionHistory LatestValidPredictionHistory ON LatestValidPredictionHistoryByUser.LatestValidPredictionHistoryID = LatestValidPredictionHistory.PredictionHistoryID
	WHERE
		Prediction.MatchID = @MatchID
		AND CurrentPredictionHistory.PredictionDateTime > Match.MatchDateTime;

	-- Set the Score and GoalDifference for every prediction for this Match
	UPDATE
		Prediction
	SET
		Score = 
			CASE 
				-- Unplayed match
				WHEN Match.MatchPlayed = 0 THEN NULL
				-- 3-pointer (perfect prediction)
				WHEN Prediction.HomeTeamGoals = Match.HomeTeamGoals AND Prediction.AwayTeamGoals = Match.AwayTeamGoals THEN 3
				-- Imperfect home win (1 or 2 points based on accuracy and competition settings)
				WHEN Match.HomeTeamGoals > Match.AwayTeamGoals AND Prediction.HomeTeamGoals > Prediction.AwayTeamGoals THEN 1 + (CASE WHEN Competition.AllowTwoPointers = 1 AND Match.HomeTeamGoals = Prediction.HomeTeamGoals THEN 1 ELSE 0 END)
				-- Imperfect away win (1 or 2 points based on accuracy and competition settings)
				WHEN Match.AwayTeamGoals > Match.HomeTeamGoals AND Prediction.AwayTeamGoals > Prediction.HomeTeamGoals THEN 1 + (CASE WHEN Competition.AllowTwoPointers = 1 AND Match.AwayTeamGoals = Prediction.AwayTeamGoals THEN 1 ELSE 0 END)
				-- Imperfect draw (1 point)
				WHEN Match.HomeTeamGoals = Match.AwayTeamGoals AND Prediction.HomeTeamGoals = Prediction.AwayTeamGoals THEN 1  
			ELSE
				0
			END
		, GoalDifference = CASE WHEN Match.MatchPlayed = 0 THEN NULL ELSE ((ABS(Prediction.HomeTeamGoals - Match.HomeTeamGoals) + ABS(Prediction.AwayTeamGoals - Match.AwayTeamGoals)) * -1)  END
	FROM
		Prediction
		INNER JOIN Match ON Prediction.MatchID = Match.MatchID
		INNER JOIN Competition ON Match.CompetitionID = Competition.CompetitionID
	WHERE
		Match.MatchID = @MatchID
		AND Prediction.Invalid = 0;
END;
