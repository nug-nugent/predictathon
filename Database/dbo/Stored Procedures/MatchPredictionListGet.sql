-- =============================================
-- Author:		David Huggett
-- Create date: 22/9/11
-- Description:	Returns a list of predictions by match
-- =============================================
CREATE PROCEDURE [dbo].[MatchPredictionListGet]
	@MatchID UNIQUEIDENTIFIER
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT
		CASE WHEN Prediction.PredictionID IS NULL THEN CAST(0x0 AS UNIQUEIDENTIFIER) ELSE Prediction.PredictionID END AS PredictionID
		, [User].Username
		, [User].UserID
		, Prediction.HomeTeamGoals
		, Prediction.AwayTeamGoals
		, Prediction.Score
	FROM
		UserCompetition
		INNER JOIN Match ON UserCompetition.CompetitionID = Match.CompetitionID AND Match.MatchID = @MatchID
		INNER JOIN [User] ON [User].UserID = UserCompetition.UserID
		LEFT JOIN Prediction ON Prediction.MatchID = Match.MatchID AND Prediction.UserID = [User].UserID AND Prediction.Invalid = 0
	ORDER BY
		Prediction.Score DESC
		, Prediction.GoalDifference DESC -- GD is always 0 or negative; 0 is the optimal result
		, [User].Username;
END;