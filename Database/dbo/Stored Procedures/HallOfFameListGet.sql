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
		, Winner = ISNULL(Winner.Username, HallOfFame.Winner)
		, HallOfFame.WinnerUserID
		, SecondPlace = ISNULL(SecondPlace.Username, HallOfFame.SecondPlace)
		, HallOfFame.SecondPlaceUserID
		, ThirdPlace = ISNULL(ThirdPlace.Username, HallOfFame.ThirdPlace)
		, HallOfFame.ThirdPlaceUserID
	FROM
		HallOfFame
		LEFT JOIN [User] Winner ON HallOfFame.WinnerUserID = Winner.UserID
		LEFT JOIN [User] SecondPlace ON HallOfFame.SecondPlaceUserID = SecondPlace.UserID
		LEFT JOIN [User] ThirdPlace ON HallOfFame.ThirdPlaceUserID = ThirdPlace.UserID
	ORDER BY
		HallOfFame.EndDate DESC
END
