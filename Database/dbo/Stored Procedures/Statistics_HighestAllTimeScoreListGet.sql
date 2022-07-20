-- =============================================
-- Author:		David Huggett
-- Create date: 09/08/2017
-- Description:	Returns the top 10 total all-time points per predictor
-- =============================================
CREATE PROCEDURE [dbo].[Statistics_HighestAllTimeScoreListGet]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	-- Total all-time points
	SELECT TOP 10
		[User].Username
		, [User].UserID
		, TotalScore = SUM(Prediction.Score)
	FROM
		[User]
		INNER JOIN Prediction ON [User].UserID = Prediction.UserID
	GROUP BY
		[User].Username
		, [User].UserID
	ORDER BY
		SUM(Prediction.Score) DESC;
END
