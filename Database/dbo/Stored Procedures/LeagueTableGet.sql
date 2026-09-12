/* ==============================================================================================================
	Description
		Returns the league table for a given competition

		When @IncludePositionChange is set, PreviousLeaguePosition carries where each user stood
		before the current match week - the move the arrow beside their row reports. "The current
		match week" is the Friday-starting week of the most recently played match, not whatever week
		the calendar happens to be in: a table read on the Wednesday after a Saturday's results still
		shows that Saturday's movement rather than going all-square, and nothing has to have been
		snapshotted on the right day for the arrows to mean anything.

		Both rankings come off the same aggregate in one pass and are ordered the same way - points,
		then total goal difference, then 3-, 2- and 1-pointers, then username - so the table a user
		is compared against can't be ranked by a different rule from the one they're standing in.

	Update History
		08/10/2011 - DH - Created
		12/09/2026 - DH - Position change derived from the current match week, rather than from
						  whichever UserCompetitionLeagueHistory snapshot happened to precede today
============================================================================================================== */
CREATE PROCEDURE [dbo].[LeagueTableGet]
	@CompetitionID UNIQUEIDENTIFIER
	, @DateFrom DATE = NULL
	, @DateTo DATE = NULL
	, @IncludePositionChange BIT = 0
AS
BEGIN
	SET NOCOUNT ON;

	-- The Friday the current match week began on. Matches before it make up the "before" table the
	-- arrows measure the move from, so a NULL here means no comparison: either the caller didn't ask
	-- for one, or there's nothing to compare against because no match was played before this week.
	DECLARE @CurrentWeekStart DATE = NULL;

	IF @IncludePositionChange = 1
	BEGIN
		DECLARE @LatestPlayedDate DATE = (
			SELECT
				MAX(CAST(m.MatchDateTime AS DATE))
			FROM
				[dbo].[Match] AS m
			WHERE
				m.CompetitionID = @CompetitionID
				AND m.MatchPlayed = 1
				AND (@DateFrom IS NULL OR CAST(m.MatchDateTime AS DATE) >= @DateFrom)
				AND (@DateTo IS NULL OR CAST(m.MatchDateTime AS DATE) <= @DateTo));

		-- Counted from a known Friday so the answer doesn't depend on the connection's DATEFIRST or
		-- language. Mirrors CompetitionService.MatchWeekStart - keep the two in step.
		SET @CurrentWeekStart = DATEADD(DAY, -(((DATEDIFF(DAY, CAST('19900105' AS DATE), @LatestPlayedDate) % 7) + 7) % 7), @LatestPlayedDate);

		IF NOT EXISTS (
			SELECT
				m.MatchID
			FROM
				[dbo].[Match] AS m
			WHERE
				m.CompetitionID = @CompetitionID
				AND m.MatchPlayed = 1
				AND CAST(m.MatchDateTime AS DATE) < @CurrentWeekStart
				AND (@DateFrom IS NULL OR CAST(m.MatchDateTime AS DATE) >= @DateFrom))
		BEGIN
			SET @CurrentWeekStart = NULL;
		END;
	END;

	WITH Scored AS (
		-- One row per registrant per played match in range, plus a NULL match row so that a user who
		-- has never predicted still reaches the table at all. BeforeThisWeek flags the rows the
		-- "before" ranking is built from.
		SELECT
			UserID = [User].Id
			, Username = [User].UserName
			, [User].ImageUploaded
			, p.PredictionID
			, p.Score
			, p.GoalDifference
			, [Match].MatchID
			, BeforeThisWeek = CASE WHEN [Match].MatchDate < @CurrentWeekStart THEN 1 ELSE 0 END
		FROM
			[Identity].[Users] AS [User]
			INNER JOIN [dbo].[UserCompetition] AS uc ON [User].Id = uc.UserID
			CROSS JOIN (
				SELECT
					m.MatchID
					, MatchDate = CAST(m.MatchDateTime AS DATE)
				FROM
					[dbo].[Match] AS m
				WHERE
					m.CompetitionID = @CompetitionID
					AND m.MatchPlayed = 1
					AND (@DateFrom IS NULL OR CAST(m.MatchDateTime AS DATE) >= @DateFrom)
					AND (@DateTo IS NULL OR CAST(m.MatchDateTime AS DATE) <= @DateTo)
				UNION ALL
				SELECT
					MatchID = NULL
					, MatchDate = NULL
				) AS [Match]
			LEFT JOIN [dbo].[Prediction] AS p ON [Match].MatchID = p.MatchID AND [User].Id = p.UserID
		WHERE
			uc.CompetitionID = @CompetitionID
	),
	Aggregated AS (
		SELECT
			UserID
			, Username
			, ImageUploaded
			, Score = ISNULL(SUM(Score), CAST(0 AS INT))
			, GoalDifference = ISNULL(SUM(GoalDifference), CAST(0 AS INT))
			, AverageGoalDifference = CAST(ISNULL(AVG(CAST(GoalDifference AS DECIMAL(9,2))), CAST(0 AS DECIMAL(9,2))) AS DECIMAL(9,2))
			, ThreePointers = SUM(CASE WHEN Score = 3 THEN 1 ELSE 0 END)
			, TwoPointers = SUM(CASE WHEN Score = 2 THEN 1 ELSE 0 END)
			, OnePointers = SUM(CASE WHEN Score = 1 THEN 1 ELSE 0 END)
			, NoPointers = SUM(CASE WHEN Score = 0 THEN 1 ELSE 0 END)
			, NoPredictions = SUM(CASE WHEN PredictionID IS NULL AND MatchID IS NOT NULL THEN 1 ELSE 0 END)
			, BeforeScore = ISNULL(SUM(CASE WHEN BeforeThisWeek = 1 THEN Score END), CAST(0 AS INT))
			, BeforeGoalDifference = ISNULL(SUM(CASE WHEN BeforeThisWeek = 1 THEN GoalDifference END), CAST(0 AS INT))
			, BeforeThreePointers = SUM(CASE WHEN BeforeThisWeek = 1 AND Score = 3 THEN 1 ELSE 0 END)
			, BeforeTwoPointers = SUM(CASE WHEN BeforeThisWeek = 1 AND Score = 2 THEN 1 ELSE 0 END)
			, BeforeOnePointers = SUM(CASE WHEN BeforeThisWeek = 1 AND Score = 1 THEN 1 ELSE 0 END)
		FROM
			Scored
		GROUP BY
			UserID
			, Username
			, ImageUploaded
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
		, PreviousLeaguePosition = CASE WHEN @CurrentWeekStart IS NULL THEN NULL ELSE ROW_NUMBER() OVER(ORDER BY
											a.BeforeScore DESC
											, a.BeforeGoalDifference DESC
											, a.BeforeThreePointers DESC
											, a.BeforeTwoPointers DESC
											, a.BeforeOnePointers DESC
											, a.Username
										) END
		, a.Score
		, a.AverageGoalDifference
		, a.ThreePointers
		, a.TwoPointers
		, a.OnePointers
		, a.NoPointers
		, a.NoPredictions
	FROM
		Aggregated AS a
	ORDER BY
		LeaguePosition;
END;
