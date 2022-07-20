-- =============================================
-- Author:		David Huggett
-- Create date: 19/10/2011
-- Description:	Clears all existing UserCompetitionLeagueHistory entries for the given date and competition, then populates the data,
--				which comprises a snapshot of how the league looked on @Date.
--				None of this is relevant if there were no matches on the given date.
-- =============================================
CREATE PROCEDURE [dbo].[UserCompetitionLeagueHistorySet]
	@Date DATE
	, @CompetitionID UNIQUEIDENTIFIER
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	--when was the table last updated for this competition?
	DECLARE @MaxLeagueHistoryDate DATE = ISNULL((SELECT 
												MAX([Date]) 
											FROM	
												UserCompetitionLeagueHistory	
												INNER JOIN UserCompetition ON UserCompetitionLeagueHistory.UserCompetitionID = UserCompetition.UserCompetitionID 
											WHERE 
												UserCompetition.CompetitionID = @CompetitionID), DATEADD(DAY, -7, GETDATE()))

	--if there've been any matches since that date, get some data in there...
	IF EXISTS(SELECT MatchID FROM Match WHERE Match.CompetitionID = @CompetitionID AND Match.MatchPlayed = 1 AND Match.MatchDateTime >= @MaxLeagueHistoryDate)
	BEGIN
		DELETE
			UserCompetitionLeagueHistory
		FROM
			UserCompetitionLeagueHistory
			INNER JOIN UserCompetition ON UserCompetitionLeagueHistory.UserCompetitionID = UserCompetition.UserCompetitionID
		WHERE
			[Date] = @Date
			AND UserCompetition.CompetitionID = @CompetitionID

		INSERT
			UserCompetitionLeagueHistory
			(
				UserCompetitionLeagueHistoryID
				, UserCompetitionID
				, [Date]
				, LeaguePosition
				, Score
				, AverageGoalDifference		
				, TotalGoalDifference		
			)
		SELECT
			NEWID()
			, UserCompetitionID
			, @Date
			, LeaguePosition
			, Score
			, AverageGoalDifference
			, TotalGoalDifference
		FROM
			UserCompetition
			INNER JOIN (
				SELECT
					[User].UserID
					, LeaguePosition = ROW_NUMBER() OVER(ORDER BY 
														ISNULL(SUM(Prediction.Score), 0) DESC
														, ISNULL(SUM(Prediction.GoalDifference), 0) DESC
														, SUM(CASE WHEN Prediction.Score = 3 THEN 1 ELSE 0 END) DESC
														, SUM(CASE WHEN Prediction.Score = 2 THEN 1 ELSE 0 END) DESC
														, SUM(CASE WHEN Prediction.Score = 1 THEN 1 ELSE 0 END) DESC
														, [User].Username
													)
					, Score = ISNULL(SUM(Prediction.Score), 0)
					, AverageGoalDifference = CAST(ISNULL(AVG(CAST(Prediction.GoalDifference AS DECIMAL(9,2))), 0) AS DECIMAL(9,2))
					, TotalGoalDifference = SUM(ISNULL(Prediction.GoalDifference, 0))
					, ThreePointers = SUM(CASE WHEN Prediction.Score = 3 THEN 1 ELSE 0 END)
					, TwoPointers = SUM(CASE WHEN Prediction.Score = 2 THEN 1 ELSE 0 END)
					, OnePointers = SUM(CASE WHEN Prediction.Score = 1 THEN 1 ELSE 0 END)
					, NoPointers = SUM(CASE WHEN Prediction.Score = 0 THEN 1 ELSE 0 END)
					, NoPredictions = SUM(CASE WHEN Prediction.PredictionID IS NULL THEN 1 ELSE 0 END)
				FROM
					[User]
					CROSS JOIN Match
					LEFT JOIN Prediction ON Match.MatchID = Prediction.MatchID AND [User].UserID = Prediction.UserID
				WHERE
					CAST(Match.MatchDateTime AS DATE) <= @Date
					AND Match.CompetitionID = @CompetitionID
				GROUP BY
					[User].Username
					, [User].UserID	
			) LeagueTable ON UserCompetition.UserID = LeagueTable.UserID
		WHERE
			UserCompetition.CompetitionID = @CompetitionID
		ORDER BY LeaguePosition
	END
END