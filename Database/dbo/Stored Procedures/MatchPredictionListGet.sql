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
		Prediction.PredictionID
		, [User].Username
		, [User].UserID
		, Prediction.HomeTeamGoals
		, Prediction.AwayTeamGoals
		, Prediction.Score
	FROM
		Match
		INNER JOIN Prediction ON Match.MatchID = Prediction.MatchID
		INNER JOIN [User] ON Prediction.UserID = [User].UserID
	WHERE
		Match.MatchID = @MatchID
	ORDER BY
		Prediction.Score DESC
		, Prediction.GoalDifference DESC -- GD is always 0 or negative; 0 is the optimal result
		, [User].Username;
END;