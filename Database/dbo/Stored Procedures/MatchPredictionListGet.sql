-- =============================================
-- Author:		David Huggett
-- Create date: 22/9/11
-- Description:	Returns a list of predictions by match
-- =============================================
CREATE PROCEDURE [dbo].[MatchPredictionListGet]
	@MatchID UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		CASE WHEN p.PredictionID IS NULL THEN CAST(0x0 AS UNIQUEIDENTIFIER) ELSE p.PredictionID END AS PredictionID
		, [User].UserName AS Username
		, [User].Id AS UserID
		, p.HomeTeamGoals
		, p.AwayTeamGoals
		, p.Score
	FROM
		[dbo].[UserCompetition] AS uc
		INNER JOIN [dbo].[Match] AS m ON uc.CompetitionID = m.CompetitionID AND m.MatchID = @MatchID
		INNER JOIN [Identity].[Users] AS [User] ON [User].Id = uc.UserID
		LEFT JOIN [dbo].[Prediction] AS p ON p.MatchID = m.MatchID AND p.UserID = [User].Id AND p.Invalid = 0
	ORDER BY
		p.Score DESC
		, p.GoalDifference DESC -- GD is always 0 or negative; 0 is the optimal result
		, [User].UserName;
END;