-- =============================================
-- Author:		David Huggett
-- Create date: 19/12/2011
-- Description:	Returns all records in the HallOfFame table
-- =============================================
CREATE PROCEDURE [dbo].[HallOfFameListGet]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		hof.HallOfFameID
		, hof.CompetitionID
		, hof.CompetitionName
		, hof.EndDate
		, hof.ImageFilename
		, Winner = ISNULL(Winner.UserName, CAST(hof.Winner AS NVARCHAR(256)))
		, hof.WinnerUserID
		, SecondPlace = ISNULL(SecondPlace.UserName, CAST(hof.SecondPlace AS NVARCHAR(256)))
		, hof.SecondPlaceUserID
		, ThirdPlace = ISNULL(ThirdPlace.UserName, CAST(hof.ThirdPlace AS NVARCHAR(256)))
		, hof.ThirdPlaceUserID
	FROM
		[dbo].[HallOfFame] AS hof
		LEFT JOIN [Identity].[Users] Winner ON hof.WinnerUserID = Winner.Id
		LEFT JOIN [Identity].[Users] SecondPlace ON hof.SecondPlaceUserID = SecondPlace.Id
		LEFT JOIN [Identity].[Users] ThirdPlace ON hof.ThirdPlaceUserID = ThirdPlace.Id
	ORDER BY
		hof.EndDate DESC;
END;