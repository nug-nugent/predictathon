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
		Winners.Username
		, Winners.UserID
		, Winners.Wins
		, Winners.SecondPlaces
		, Winners.ThirdPlaces
	FROM
		(
		SELECT
			[User].UserName AS Username
			, [User].Id AS UserID
			, Wins = (SELECT COUNT(1) FROM [dbo].[HallOfFame] AS hof WHERE hof.WinnerUserID = [User].Id)
			, SecondPlaces = (SELECT COUNT(1) FROM [dbo].[HallOfFame] AS hof WHERE hof.SecondPlaceUserID = [User].Id)
			, ThirdPlaces = (SELECT COUNT(1) FROM [dbo].[HallOfFame] AS hof WHERE hof.ThirdPlaceUserID = [User].Id)
		FROM
			[Identity].[Users] AS [User]
		GROUP BY
			[User].UserName
			, [User].Id) Winners
	WHERE
		Winners.Wins + Winners.SecondPlaces + Winners.ThirdPlaces > 0
	ORDER BY
		Winners.Wins DESC
		, Winners.SecondPlaces DESC
		, Winners.ThirdPlaces DESC;
END;