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
											ISNULL(SUM(Prediction.Score), 0) DESC --Total points
											, ISNULL(SUM(Prediction.GoalDifference), 0) DESC --GD
											, SUM(CASE WHEN Prediction.Score = 3 THEN 1 ELSE 0 END) DESC --3-pointers
											, SUM(CASE WHEN Prediction.Score = 2 THEN 1 ELSE 0 END) DESC --2-pointers
											, SUM(CASE WHEN Prediction.Score = 1 THEN 1 ELSE 0 END) DESC --1-pointers
											, [User].UserName --Username
										)
		, PreviousLeaguePosition = CASE WHEN @DateForComparison IS NULL THEN NULL ELSE (
									SELECT TOP 1
										LeaguePosition
									FROM
										UserCompetitionLeagueHistory
									WHERE
										UserCompetitionLeagueHistory.UserCompetitionID = UserCompetition.UserCompetitionID
										AND UserCompetitionLeagueHistory.Date < @DateForComparison
									ORDER BY
										UserCompetitionLeagueHistory.Date DESC) END
		, Score = ISNULL(SUM(Prediction.Score), 0)
		, AverageGoalDifference = CAST(ISNULL(AVG(CAST(Prediction.GoalDifference AS DECIMAL(9,2))), 0) AS DECIMAL(9,2))
		, ThreePointers = SUM(CASE WHEN Prediction.Score = 3 THEN 1 ELSE 0 END)
		, TwoPointers = SUM(CASE WHEN Prediction.Score = 2 THEN 1 ELSE 0 END)
		, OnePointers = SUM(CASE WHEN Prediction.Score = 1 THEN 1 ELSE 0 END)
		, NoPointers = SUM(CASE WHEN Prediction.Score = 0 THEN 1 ELSE 0 END)
		, NoPredictions = SUM(CASE WHEN Prediction.PredictionID IS NULL AND Match.MatchID IS NOT NULL THEN 1 ELSE 0 END)
	FROM
		[Identity].[Users] AS [User]
		INNER JOIN UserCompetition ON [User].Id = UserCompetition.UserID
		CROSS JOIN (
			SELECT
				Match.MatchID
			FROM
				Match
			WHERE
				Match.CompetitionID = @CompetitionID
				AND Match.MatchPlayed = 1
				AND (@DateFrom IS NULL OR CAST(Match.MatchDateTime AS DATE) >= @DateFrom)
				AND (@DateTo IS NULL OR CAST(Match.MatchDateTime AS DATE) <= @DateTo)
			UNION
			SELECT
				MatchID = NULL
			) Match
		LEFT JOIN Prediction ON Match.MatchID = Prediction.MatchID AND [User].Id = Prediction.UserID
	WHERE
		UserCompetition.CompetitionID = @CompetitionID
	GROUP BY
		[User].UserName
		, [User].Id
		, UserCompetition.UserCompetitionID
	ORDER BY
		LeaguePosition;
END;