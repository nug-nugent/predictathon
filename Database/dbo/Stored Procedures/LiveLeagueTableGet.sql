/* ==============================================================================================================
	Description
		Returns a competition's league table as it would stand right now if every match in play ended
		on its provisional live score - the real table with the live scores applied, not the confirmed
		table with a hint attached. Every column is the live-applied value: points, goal difference,
		the 3/2/1/0 counts, and the position itself.

		PreviousLeaguePosition carries where the user stands on confirmed results alone, so the
		position-change arrow reads exactly as it does on the League page: how you got to the row
		you're on. Somebody sitting top can only have climbed there or held it - they can't be shown
		falling out of a position they currently occupy.

		LivePoints is reported alongside so a reader can see how much of the total is provisional.

		Only matches with a live score and no confirmed result count as "in play". Once a result is
		entered, MatchPredictionScoreSet writes the real points onto the predictions and they land in
		the confirmed totals, so counting them here as well would double them.

		Both orderings are the one LeagueTableGet uses - points, then total goal difference, then 3-,
		2- and 1-pointers, then username. Ranking in one place, in one language, is the point: a live
		table that ordered users differently from the real one would be worse than no live table.

		Live predictions are scored through dbo.PredictionScoreGet, which is where the scoring rule
		lives, so this procedure can't drift away from it.

	Update History
		30/08/2026 - DH - Created
============================================================================================================== */
CREATE PROCEDURE [dbo].[LiveLeagueTableGet]
	@CompetitionID UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

	WITH Confirmed AS (
		-- The league table as LeagueTableGet computes it, minus the position itself and the
		-- previous-position comparison, which this procedure derives differently.
		SELECT
			UserID = [User].Id
			, Username = [User].UserName
			, [User].ImageUploaded
			, Score = ISNULL(SUM(p.Score), 0)
			, GoalDifference = ISNULL(SUM(p.GoalDifference), 0)
			, ScoredPredictions = COUNT(p.GoalDifference)
			, ThreePointers = SUM(CASE WHEN p.Score = 3 THEN 1 ELSE 0 END)
			, TwoPointers = SUM(CASE WHEN p.Score = 2 THEN 1 ELSE 0 END)
			, OnePointers = SUM(CASE WHEN p.Score = 1 THEN 1 ELSE 0 END)
			, NoPointers = SUM(CASE WHEN p.Score = 0 THEN 1 ELSE 0 END)
			, NoPredictions = SUM(CASE WHEN p.PredictionID IS NULL AND [Match].MatchID IS NOT NULL THEN 1 ELSE 0 END)
		FROM
			[Identity].[Users] AS [User]
			INNER JOIN [dbo].[UserCompetition] AS uc ON [User].Id = uc.UserID
			CROSS JOIN (
				SELECT
					m.MatchID
				FROM
					[dbo].[Match] AS m
				WHERE
					m.CompetitionID = @CompetitionID
					AND m.MatchPlayed = 1
				UNION
				SELECT
					MatchID = NULL
				) [Match]
			LEFT JOIN [dbo].[Prediction] AS p ON [Match].MatchID = p.MatchID AND [User].Id = p.UserID
		WHERE
			uc.CompetitionID = @CompetitionID
		GROUP BY
			[User].Id
			, [User].UserName
			, [User].ImageUploaded
	),
	MatchesInPlay AS (
		SELECT
			m.MatchID
			, comp.AllowTwoPointers
			, ls.HomeTeamGoals
			, ls.AwayTeamGoals
		FROM
			[dbo].[Match] AS m
			INNER JOIN [dbo].[Competition] AS comp ON comp.CompetitionID = m.CompetitionID
			INNER JOIN [dbo].[MatchLiveScore] AS ls ON ls.MatchID = m.MatchID
		WHERE
			m.CompetitionID = @CompetitionID
			AND m.MatchPlayed = 0
	),
	LivePredictions AS (
		-- One row per prediction on a match in play, scored as if the live score were the result.
		-- Scoring each prediction individually rather than in the aggregates below is what lets the
		-- pointer counts be built from the same numbers as the points.
		SELECT
			p.UserID
			, LiveScore = s.Score
			, LiveGoalDifference = s.GoalDifference
		FROM
			MatchesInPlay AS ip
			INNER JOIN [dbo].[Prediction] AS p ON p.MatchID = ip.MatchID AND p.Invalid = 0
			CROSS APPLY [dbo].[PredictionScoreGet](p.HomeTeamGoals, p.AwayTeamGoals, ip.HomeTeamGoals, ip.AwayTeamGoals, ip.AllowTwoPointers) AS s
	),
	Live AS (
		SELECT
			UserID
			, LivePoints = ISNULL(SUM(LiveScore), 0)
			, LiveGoalDifference = ISNULL(SUM(LiveGoalDifference), 0)
			, LiveScoredPredictions = COUNT(LiveGoalDifference)
			, LiveThreePointers = SUM(CASE WHEN LiveScore = 3 THEN 1 ELSE 0 END)
			, LiveTwoPointers = SUM(CASE WHEN LiveScore = 2 THEN 1 ELSE 0 END)
			, LiveOnePointers = SUM(CASE WHEN LiveScore = 1 THEN 1 ELSE 0 END)
			, LiveNoPointers = SUM(CASE WHEN LiveScore = 0 THEN 1 ELSE 0 END)
			, LivePredictions = COUNT(*)
		FROM
			LivePredictions
		GROUP BY
			UserID
	),
	Applied AS (
		SELECT
			t.UserID
			, t.Username
			, t.ImageUploaded
			, ConfirmedScore = t.Score
			, ConfirmedGoalDifference = t.GoalDifference
			, ConfirmedThreePointers = t.ThreePointers
			, ConfirmedTwoPointers = t.TwoPointers
			, ConfirmedOnePointers = t.OnePointers
			, LivePoints = ISNULL(l.LivePoints, 0)
			, Score = t.Score + ISNULL(l.LivePoints, 0)
			, GoalDifference = t.GoalDifference + ISNULL(l.LiveGoalDifference, 0)
			, ScoredPredictions = t.ScoredPredictions + ISNULL(l.LiveScoredPredictions, 0)
			, ThreePointers = t.ThreePointers + ISNULL(l.LiveThreePointers, 0)
			, TwoPointers = t.TwoPointers + ISNULL(l.LiveTwoPointers, 0)
			, OnePointers = t.OnePointers + ISNULL(l.LiveOnePointers, 0)
			, NoPointers = t.NoPointers + ISNULL(l.LiveNoPointers, 0)
			-- A match in play that a user never predicted is a missed prediction in the making, and
			-- counts as one here for the same reason the points do.
			, NoPredictions = t.NoPredictions + ((SELECT COUNT(*) FROM MatchesInPlay) - ISNULL(l.LivePredictions, 0))
		FROM
			Confirmed AS t
			LEFT JOIN Live AS l ON l.UserID = t.UserID
	)
	SELECT
		a.Username
		, a.UserID
		, a.ImageUploaded
		, LeaguePosition = ROW_NUMBER() OVER(ORDER BY
											a.Score DESC --Total points
											, a.GoalDifference DESC --GD
											, a.ThreePointers DESC --3-pointers
											, a.TwoPointers DESC --2-pointers
											, a.OnePointers DESC --1-pointers
											, a.Username --Username
										)
		-- Where they stand on confirmed results alone: what the arrow measures the move from.
		, PreviousLeaguePosition = ROW_NUMBER() OVER(ORDER BY
											a.ConfirmedScore DESC
											, a.ConfirmedGoalDifference DESC
											, a.ConfirmedThreePointers DESC
											, a.ConfirmedTwoPointers DESC
											, a.ConfirmedOnePointers DESC
											, a.Username
										)
		, a.Score
		, AverageGoalDifference = CAST(CASE WHEN a.ScoredPredictions = 0 THEN CAST(0 AS DECIMAL(9,2))
			ELSE CAST(a.GoalDifference AS DECIMAL(9,2)) / a.ScoredPredictions END AS DECIMAL(9,2))
		, a.ThreePointers
		, a.TwoPointers
		, a.OnePointers
		, a.NoPointers
		, a.NoPredictions
		, a.LivePoints
	FROM
		Applied AS a
	ORDER BY
		LeaguePosition;
END;
