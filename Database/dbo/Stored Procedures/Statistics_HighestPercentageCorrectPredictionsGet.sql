-- =============================================
-- Author:		David Huggett
-- Create date: 09/08/2017
-- Description:	Returns the highest percentage of 'correct' predictions (win/lose/draw only)
-- =============================================
CREATE PROCEDURE [dbo].[Statistics_HighestPercentageCorrectPredictionsGet]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	--	Highest percentage of 'correct' predictions (win/lose/draw only)
	 SELECT TOP 10
 		Username
		, UserID
		, CorrectPredictionPercentage = (CAST(CorrectPredictions AS DECIMAL(9, 2)) / CAST(TotalPredictions AS DECIMAL(9, 2))) * 100
	FROM
		(
		SELECT
			[User].Username
			, [User].UserID
			, TotalPredictions = COUNT(1)
			, CorrectPredictions = SUM(CASE WHEN Prediction.Score > 0 THEN 1 ELSE 0 END)
		FROM
			[User]
			INNER JOIN Prediction ON [User].UserID = Prediction.UserID
		WHERE
			Prediction.Score IS NOT NULL
		GROUP BY
			[User].Username
			, [User].UserID
		) PredictionCount
	WHERE
		PredictionCount.CorrectPredictions > 0
	ORDER BY
		CAST(CorrectPredictions AS DECIMAL(9, 2)) / CAST(TotalPredictions AS DECIMAL(9, 2)) DESC;
END
