-- =============================================
-- Author:		David Huggett
-- Create date: 09/08/2017
-- Description:	Returns the top 10 users to have predicted the most matches altogether
-- =============================================
CREATE PROCEDURE [dbo].[Statistics_MostMatchesPredictedUserListGet]
AS
BEGIN
	SET NOCOUNT ON;

	-- Users to have predicted the most matches altogether
	SELECT TOP 10
		[User].UserName AS Username
		, [User].Id AS UserID
		, TotalPredictions = COUNT(1)
	FROM
		[Identity].[Users] AS [User]
		INNER JOIN [dbo].[Prediction] AS p ON [User].Id = p.UserID
	GROUP BY
		[User].UserName
		, [User].Id
	ORDER BY
		COUNT(1) DESC;
END;