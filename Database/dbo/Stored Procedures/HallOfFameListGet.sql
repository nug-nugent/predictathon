-- =============================================
-- Author:		David Huggett
-- Create date: 19/12/2011
-- Description:	Returns all records in the HallOfFame table
-- =============================================
CREATE PROCEDURE [dbo].[HallOfFameListGet]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT
		HallOfFame.HallOfFameID
		, HallOfFame.CompetitionID
		, HallOfFame.CompetitionName
		, HallOfFame.EndDate
		, HallOfFame.ImageFilename
		, Winner = ISNULL(Winner.UserName, HallOfFame.Winner)
		, HallOfFame.WinnerUserID
		, SecondPlace = ISNULL(SecondPlace.UserName, HallOfFame.SecondPlace)
		, HallOfFame.SecondPlaceUserID
		, ThirdPlace = ISNULL(ThirdPlace.UserName, HallOfFame.ThirdPlace)
		, HallOfFame.ThirdPlaceUserID
	FROM
		HallOfFame
		LEFT JOIN [Identity].[Users] Winner ON HallOfFame.WinnerUserID = Winner.Id
		LEFT JOIN [Identity].[Users] SecondPlace ON HallOfFame.SecondPlaceUserID = SecondPlace.Id
		LEFT JOIN [Identity].[Users] ThirdPlace ON HallOfFame.ThirdPlaceUserID = ThirdPlace.Id
	ORDER BY
		HallOfFame.EndDate DESC
END
