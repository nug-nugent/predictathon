-- =============================================
-- Author:		David Huggett
-- Create date: 31/08/2026
-- Description:	Returns each user's competition wins, grouped into trophies. Wins in the same
--              competition series collapse into one trophy carrying a count; wins in a
--              competition with no series stay individual, named after the competition itself.
--              Pass @UserID for one user, or leave it NULL for everybody (the message board
--              needs a page's worth of authors in one round trip).
-- =============================================
CREATE PROCEDURE [dbo].[UserTrophiesGet]
	@UserID UNIQUEIDENTIFIER = NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- A series is resolved from the Hall of Fame row first and the competition second: the oldest
	-- entries predate the Competition table entirely and carry no CompetitionID to resolve through.
	WITH CompetitionWins AS
	(
		SELECT
			UserID = hof.WinnerUserID
			, HallOfFameID = hof.HallOfFameID
			, CompetitionSeriesID = COALESCE(hof.CompetitionSeriesID, c.CompetitionSeriesID)
			, CompetitionName = COALESCE(hof.CompetitionName, c.CompetitionName)
			, EndDate = hof.EndDate
		FROM
			[dbo].[HallOfFame] AS hof
			LEFT JOIN [dbo].[Competition] AS c ON hof.CompetitionID = c.CompetitionID
		WHERE
			hof.WinnerUserID IS NOT NULL
			AND (@UserID IS NULL OR hof.WinnerUserID = @UserID)
	)
	SELECT
		UserID = Wins.UserID
		, CompetitionSeriesID = Wins.CompetitionSeriesID
		, Name = COALESCE(cs.SeriesName, Wins.CompetitionName, 'Competition')
		, ShortName = cs.ShortName
		, BadgeIcon = cs.BadgeIcon
		, BadgeColour = cs.BadgeColour
		-- Series sort by their own order; series-less one-offs fall in behind all of them.
		, DisplayOrder = ISNULL(cs.DisplayOrder, 1000)
		, WinCount = COUNT(1)
		, MostRecentWin = MAX(Wins.EndDate)
		, Years = STRING_AGG(CAST(YEAR(Wins.EndDate) AS VARCHAR(4)), ', ') WITHIN GROUP (ORDER BY YEAR(Wins.EndDate))
	FROM
		CompetitionWins AS Wins
		LEFT JOIN [dbo].[CompetitionSeries] AS cs ON Wins.CompetitionSeriesID = cs.CompetitionSeriesID
	GROUP BY
		Wins.UserID
		, Wins.CompetitionSeriesID
		, COALESCE(cs.SeriesName, Wins.CompetitionName, 'Competition')
		, cs.ShortName
		, cs.BadgeIcon
		, cs.BadgeColour
		, cs.DisplayOrder
		-- Constant (NULL) for a series win so its rows collapse together, unique per row for a
		-- series-less one-off so each stays its own trophy.
		, CASE WHEN Wins.CompetitionSeriesID IS NULL THEN Wins.HallOfFameID END
	ORDER BY
		Wins.UserID
		, ISNULL(cs.DisplayOrder, 1000)
		, MAX(Wins.EndDate) DESC;
END;
