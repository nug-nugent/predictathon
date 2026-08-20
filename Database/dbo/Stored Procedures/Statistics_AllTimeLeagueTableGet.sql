-- =============================================
-- Author:		David Huggett
-- Create date: 20/08/2026
-- Description:	Returns the all-time league table - one row per user across every competition
--				they've ever been registered for, ranked the same way as a single competition's
--				LeagueTableGet (total points, then GD, then 3/2/1-pointer counts, then username).
-- =============================================
CREATE PROCEDURE [dbo].[Statistics_AllTimeLeagueTableGet]
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
		, Score = ISNULL(SUM(p.Score), CAST(0 AS INT))
		, AverageGoalDifference = CAST(ISNULL(AVG(CAST(p.GoalDifference AS DECIMAL(9,2))), CAST(0 AS DECIMAL(9,2))) AS DECIMAL(9,2))
		, ThreePointers = SUM(CASE WHEN p.Score = 3 THEN 1 ELSE 0 END)
		, TwoPointers = SUM(CASE WHEN p.Score = 2 THEN 1 ELSE 0 END)
		, OnePointers = SUM(CASE WHEN p.Score = 1 THEN 1 ELSE 0 END)
		, NoPointers = SUM(CASE WHEN p.Score = 0 THEN 1 ELSE 0 END)
		, NoPredictions = SUM(CASE WHEN p.PredictionID IS NULL THEN 1 ELSE 0 END)
	FROM
		[Identity].[Users] AS [User]
		INNER JOIN [dbo].[UserCompetition] AS uc ON [User].Id = uc.UserID
		INNER JOIN [dbo].[Match] AS m ON m.CompetitionID = uc.CompetitionID AND m.MatchPlayed = 1
		LEFT JOIN [dbo].[Prediction] AS p ON m.MatchID = p.MatchID AND [User].Id = p.UserID
	GROUP BY
		[User].UserName
		, [User].Id
	ORDER BY
		LeaguePosition;
END;
