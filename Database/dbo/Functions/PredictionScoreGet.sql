/* ==============================================================================================================
	Description
		The scoring rule: what one prediction is worth against one scoreline. 3 for the exact score,
		2 for the right result with the winner's goals right (where the competition allows two-pointers),
		1 for the right result, 0 for anything else.

		Written as an inline table-valued function rather than a scalar one so the optimiser folds it
		into the calling query - a scalar UDF would be a per-row call on older SQL Server versions,
		and this gets applied to every prediction in a competition.

		Callers apply it to a *scoreline*, whatever the source: dbo.MatchPredictionListGet and
		dbo.LiveLeagueTableGet both use it against a provisional MatchLiveScore. It deliberately says
		nothing about whether a match has finished - that's the caller's business.

		dbo.MatchPredictionScoreSet still carries its own copy of these rules, since it predates this
		function and rewriting the procedure that awards real points is a change worth making on its
		own. If the scoring rules change, that procedure and this function have to change together.

	Update History
		30/08/2026 - DH - Created
============================================================================================================== */
CREATE FUNCTION [dbo].[PredictionScoreGet]
(
	@PredictedHomeTeamGoals INT
	, @PredictedAwayTeamGoals INT
	, @ActualHomeTeamGoals INT
	, @ActualAwayTeamGoals INT
	, @AllowTwoPointers BIT
)
RETURNS TABLE
AS
RETURN
(
	SELECT
		Score =
			CASE
				-- 3-pointer (perfect prediction)
				WHEN @PredictedHomeTeamGoals = @ActualHomeTeamGoals AND @PredictedAwayTeamGoals = @ActualAwayTeamGoals THEN 3
				-- Imperfect home win (1 or 2 points based on accuracy and competition settings)
				WHEN @ActualHomeTeamGoals > @ActualAwayTeamGoals AND @PredictedHomeTeamGoals > @PredictedAwayTeamGoals THEN 1 + (CASE WHEN @AllowTwoPointers = 1 AND @ActualHomeTeamGoals = @PredictedHomeTeamGoals THEN 1 ELSE 0 END)
				-- Imperfect away win (1 or 2 points based on accuracy and competition settings)
				WHEN @ActualAwayTeamGoals > @ActualHomeTeamGoals AND @PredictedAwayTeamGoals > @PredictedHomeTeamGoals THEN 1 + (CASE WHEN @AllowTwoPointers = 1 AND @ActualAwayTeamGoals = @PredictedAwayTeamGoals THEN 1 ELSE 0 END)
				-- Imperfect draw (1 point)
				WHEN @ActualHomeTeamGoals = @ActualAwayTeamGoals AND @PredictedHomeTeamGoals = @PredictedAwayTeamGoals THEN 1
			ELSE
				0
			END
		, GoalDifference = ((ABS(@PredictedHomeTeamGoals - @ActualHomeTeamGoals) + ABS(@PredictedAwayTeamGoals - @ActualAwayTeamGoals)) * -1)
);
