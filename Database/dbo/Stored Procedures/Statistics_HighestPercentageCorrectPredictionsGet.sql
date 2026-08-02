-- =============================================
-- Author:		David Huggett
-- Create date: 09/08/2017
-- Description:	Returns the highest percentage of 'correct' predictions (win/lose/draw only)
-- =============================================
CREATE PROCEDURE [dbo].[Statistics_HighestPercentageCorrectPredictionsGet]
AS
BEGIN
	SET NOCOUNT ON;

	--	Highest percentage of 'correct' predictions (win/lose/draw only)
	 SELECT TOP 10
 		PredictionCount.Username
		, PredictionCount.UserID
		, CorrectPredictionPercentage = (CAST(PredictionCount.CorrectPredictions AS DECIMAL(9, 2)) / CAST(PredictionCount.TotalPredictions AS DECIMAL(9, 2))) * 100
	FROM
		(
		SELECT
			[User].UserName AS Username
			, [User].Id AS UserID
			, TotalPredictions = COUNT(1)
			, CorrectPredictions = SUM(CASE WHEN p.Score > 0 THEN 1 ELSE 0 END)
		FROM
			[Identity].[Users] AS [User]
			INNER JOIN [dbo].[Prediction] AS p ON [User].Id = p.UserID
		WHERE
			p.Score IS NOT NULL
		GROUP BY
			[User].UserName
			, [User].Id
		) PredictionCount
	WHERE
		PredictionCount.CorrectPredictions > 0
	ORDER BY
		CAST(PredictionCount.CorrectPredictions AS DECIMAL(9, 2)) / CAST(PredictionCount.TotalPredictions AS DECIMAL(9, 2)) DESC;
END;