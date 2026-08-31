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
	--
	-- TrophyKey is what decides whether two wins are the same trophy: the series for a win that has
	-- one, so its rows collapse together, and the Hall of Fame row's own id otherwise, so each
	-- series-less one-off stays a trophy in its own right.
	WITH CompetitionWins AS
	(
		SELECT
			UserID = hof.WinnerUserID
			, TrophyKey = COALESCE(hof.CompetitionSeriesID, c.CompetitionSeriesID, hof.HallOfFameID)
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
		-- The years making up this trophy, oldest first. STUFF/FOR XML PATH rather than STRING_AGG:
		-- STRING_AGG needs SQL Server 2017 and database compatibility level 110+, which not every
		-- instance this schema deploys to has. TYPE/.value keeps the concatenation from XML-escaping
		-- its own separator.
		, Years = STUFF((
			SELECT
				', ' + CAST(YEAR(Year.EndDate) AS VARCHAR(4))
			FROM
				CompetitionWins AS Year
			WHERE
				Year.UserID = Wins.UserID
				AND Year.TrophyKey = Wins.TrophyKey
			ORDER BY
				Year.EndDate
			FOR XML PATH(''), TYPE).value('.', 'VARCHAR(MAX)'), 1, 2, '')
	FROM
		CompetitionWins AS Wins
		LEFT JOIN [dbo].[CompetitionSeries] AS cs ON Wins.CompetitionSeriesID = cs.CompetitionSeriesID
	GROUP BY
		Wins.UserID
		, Wins.TrophyKey
		, Wins.CompetitionSeriesID
		, COALESCE(cs.SeriesName, Wins.CompetitionName, 'Competition')
		, cs.ShortName
		, cs.BadgeIcon
		, cs.BadgeColour
		, cs.DisplayOrder
	ORDER BY
		Wins.UserID
		, ISNULL(cs.DisplayOrder, 1000)
		, MAX(Wins.EndDate) DESC;
END;
