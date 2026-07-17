-- =============================================
-- Author:		David Huggett
-- Create date: 09/08/2017
-- Description:	Returns the most overall wins/2nds/3rds
-- =============================================
CREATE PROCEDURE [dbo].[Statistics_CompetitionWinnerListGet]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	-- Most overall wins/2nds/3rds
	SELECT
		Username
		, UserID
		, Wins
		, SecondPlaces
		, ThirdPlaces
	FROM
		(
		SELECT
			[User].UserName AS Username
			, [User].Id AS UserID
			, Wins = (SELECT COUNT(1) FROM HallOfFame WHERE WinnerUserID = [User].Id)
			, SecondPlaces = (SELECT COUNT(1) FROM HallOfFame WHERE SecondPlaceUserID = [User].Id)
			, ThirdPlaces = (SELECT COUNT(1) FROM HallOfFame WHERE ThirdPlaceUserID = [User].Id)
		FROM
			[Identity].[Users] AS [User]
		GROUP BY
			[User].UserName
			, [User].Id) Winners
	WHERE
		Wins + SecondPlaces + ThirdPlaces > 0
	ORDER BY
		Wins DESC
		, SecondPlaces DESC
		, ThirdPlaces DESC;
END;