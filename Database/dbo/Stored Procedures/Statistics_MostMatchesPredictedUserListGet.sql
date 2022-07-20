-- =============================================
-- Author:		David Huggett
-- Create date: 09/08/2017
-- Description:	Returns the top 10 users to have predicted the most matches altogether
-- =============================================
CREATE PROCEDURE [dbo].[Statistics_MostMatchesPredictedUserListGet]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	-- Users to have predicted the most matches altogether
	SELECT TOP 10
		[User].Username
		, [User].UserID
		, TotalPredictions = COUNT(1)
	FROM
		[User]
		INNER JOIN Prediction ON [User].UserID = Prediction.UserID
	GROUP BY
		[User].Username
		, [User].UserID
	ORDER BY
		COUNT(1) DESC;
END
