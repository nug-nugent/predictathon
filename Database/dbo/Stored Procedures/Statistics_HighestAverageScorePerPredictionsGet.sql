-- =============================================
-- Author:		David Huggett
-- Create date: 09/08/2017
-- Description:	Returns the highest average score per prediction
-- =============================================
CREATE PROCEDURE [dbo].[Statistics_HighestAverageScorePerPredictionsGet]
AS
BEGIN
	SET NOCOUNT ON;

	--	Highest average score per prediction
	 SELECT TOP (10)
		[User].UserName AS Username
		, [User].Id AS UserID
		, AverageScore = CAST(SUM(p.Score) AS DECIMAL(9, 2)) / COUNT(1.00)
	FROM
		[Identity].[Users] AS [User]
		INNER JOIN [dbo].[Prediction] AS p ON [User].Id = p.UserID
	GROUP BY
		[User].UserName
		, [User].Id
	ORDER BY
		CAST(SUM(p.Score) AS DECIMAL(9, 2)) / COUNT(1.00) DESC;
END;