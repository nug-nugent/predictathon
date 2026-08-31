-- =============================================
-- Author:		David Huggett
-- Create date: 22/9/11
-- Description:	Returns a list of predictions by match, each with what it scored (once the match has
--              a confirmed result) or what it is currently worth against the live score (while the
--              match is still being played), best prediction first either way.
-- =============================================
CREATE PROCEDURE [dbo].[MatchPredictionListGet]
	@MatchID UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

	-- The projected columns are computed once here so the ordering below can sort on them as well
	-- as return them; the outer SELECT hands back only what the caller's model carries.
	WITH MatchPredictions AS
	(
		SELECT
			PredictionID = CASE WHEN p.PredictionID IS NULL THEN CAST(0x0 AS UNIQUEIDENTIFIER) ELSE p.PredictionID END
			, Username = [User].UserName
			, UserID = [User].Id
			, p.HomeTeamGoals
			, p.AwayTeamGoals
			, p.Score
			, p.GoalDifference
			-- What the prediction is worth against the match's provisional live score. NULL rather than
			-- zero wherever there's nothing to project from: a match that already has a confirmed result
			-- (Score above is the real answer there), one nobody has reported a live score for yet, or a
			-- user who didn't predict. A zero would read as "worth nothing", which isn't the same thing.
			, ProjectedScore = CASE
				WHEN m.MatchPlayed = 1 THEN NULL
				WHEN ls.MatchID IS NULL THEN NULL
				WHEN p.HomeTeamGoals IS NULL OR p.AwayTeamGoals IS NULL THEN NULL
				ELSE projected.Score END
			-- Only ever an ordering key, on exactly the rows ProjectedScore applies to - so a match
			-- with a confirmed result orders on its real goal difference alone, never on how the
			-- prediction happened to sit against the last live score before full time.
			, ProjectedGoalDifference = CASE
				WHEN m.MatchPlayed = 1 THEN NULL
				WHEN ls.MatchID IS NULL THEN NULL
				WHEN p.HomeTeamGoals IS NULL OR p.AwayTeamGoals IS NULL THEN NULL
				ELSE projected.GoalDifference END
			, m.MatchPlayed
		FROM
			[dbo].[UserCompetition] AS uc
			INNER JOIN [dbo].[Match] AS m ON uc.CompetitionID = m.CompetitionID AND m.MatchID = @MatchID
			INNER JOIN [dbo].[Competition] AS comp ON comp.CompetitionID = m.CompetitionID
			INNER JOIN [Identity].[Users] AS [User] ON [User].Id = uc.UserID
			LEFT JOIN [dbo].[Prediction] AS p ON p.MatchID = m.MatchID AND p.UserID = [User].Id AND p.Invalid = 0
			LEFT JOIN [dbo].[MatchLiveScore] AS ls ON ls.MatchID = m.MatchID
			OUTER APPLY [dbo].[PredictionScoreGet](p.HomeTeamGoals, p.AwayTeamGoals, ls.HomeTeamGoals, ls.AwayTeamGoals, comp.AllowTwoPointers) AS projected
	)
	SELECT
		PredictionID
		, Username
		, UserID
		, HomeTeamGoals
		, AwayTeamGoals
		, Score
		, ProjectedScore
	FROM
		MatchPredictions
	-- Best prediction first, on whichever measure the match is actually being read by: the confirmed
	-- points once there's a result, the projection against the live score until then. Chosen on
	-- MatchPlayed rather than on which columns happen to be populated, because a prediction can carry
	-- a confirmed Score while its match is back to unplayed - a result processed and then reopened,
	-- which is what a fixture correction does - and that stale score must not outrank the projection
	-- the reader is being shown. DESC leaves NULLs last, so whoever didn't predict sits at the bottom
	-- rather than mixed in among the people who did.
	ORDER BY
		CASE WHEN MatchPlayed = 1 THEN Score ELSE ProjectedScore END DESC
		-- GD is always 0 or negative; 0 is the optimal result.
		, CASE WHEN MatchPlayed = 1 THEN GoalDifference ELSE ProjectedGoalDifference END DESC
		, Username;
END;
