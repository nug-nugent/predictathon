/* ==============================================================================================================
	Description
		Returns the league table for a given competition

	Update History
		08/10/2011 - DH - Created
============================================================================================================== */
CREATE PROCEDURE [dbo].[LeagueTableGet]
	@CompetitionID UNIQUEIDENTIFIER
	, @DateFrom DATE = NULL
	, @DateTo DATE = NULL
	, @DateForComparison DATE = NULL
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		[User].UserName AS Username
		, [User].Id AS UserID
		, LeaguePosition = ROW_NUMBER() OVER(ORDER BY
											ISNULL(SUM(p.Score), 0) DESC --Total points
											, ISNULL(SUM(p.GoalDifference), 0) DESC --GD
											, SUM(CASE WHEN p.Score = 3 THEN 1 ELSE 0 END) DESC --3-pointers
											, SUM(CASE WHEN p.Score = 2 THEN 1 ELSE 0 END) DESC --2-pointers
											, SUM(CASE WHEN p.Score = 1 THEN 1 ELSE 0 END) DESC --1-pointers
											, [User].UserName --Username
										)
		, PreviousLeaguePosition = CASE WHEN @DateForComparison IS NULL THEN NULL ELSE (
									SELECT TOP (1)
										LeaguePosition
									FROM
										[dbo].[UserCompetitionLeagueHistory] AS ulh
									WHERE
										ulh.UserCompetitionID = uc.UserCompetitionID
										AND ulh.Date < @DateForComparison
									ORDER BY
										ulh.Date DESC) END
		, Score = ISNULL(SUM(p.Score), CAST(0 AS INT))
		, AverageGoalDifference = CAST(ISNULL(AVG(CAST(p.GoalDifference AS DECIMAL(9,2))), CAST(0 AS DECIMAL(9,2))) AS DECIMAL(9,2))
		, ThreePointers = SUM(CASE WHEN p.Score = 3 THEN 1 ELSE 0 END)
		, TwoPointers = SUM(CASE WHEN p.Score = 2 THEN 1 ELSE 0 END)
		, OnePointers = SUM(CASE WHEN p.Score = 1 THEN 1 ELSE 0 END)
		, NoPointers = SUM(CASE WHEN p.Score = 0 THEN 1 ELSE 0 END)
		, NoPredictions = SUM(CASE WHEN p.PredictionID IS NULL AND Match.MatchID IS NOT NULL THEN 1 ELSE 0 END)
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
				AND (@DateFrom IS NULL OR CAST(m.MatchDateTime AS DATE) >= @DateFrom)
				AND (@DateTo IS NULL OR CAST(m.MatchDateTime AS DATE) <= @DateTo)
			UNION
			SELECT
				MatchID = NULL
			) Match
		LEFT JOIN [dbo].[Prediction] AS p ON Match.MatchID = p.MatchID AND [User].Id = p.UserID
	WHERE
		uc.CompetitionID = @CompetitionID
	GROUP BY
		[User].UserName
		, [User].Id
		, uc.UserCompetitionID
	ORDER BY
		LeaguePosition;
END;