/*
Rebuilds dbo.UserCompetitionLeagueHistory for one competition, which is what the Profile page's
position-over-time chart is drawn from.

Snapshots written before September 2026 carry two faults that dbo.UserCompetitionLeagueHistorySet
has since been fixed for: positions were ranked across every user on the site rather than across the
competition's registrants, and matches that hadn't been played yet were counted. The fix only
applies to snapshots taken from now on, so a chart of older rows stays wrong until they're rebuilt.
That's what this does.

Scope it with @CompetitionID below. Left unset it takes the most recently started competition - set
it explicitly to rebuild an older one, and run the script once per competition you care about. A
competition that hasn't played a match yet rebuilds to nothing, which is worth knowing if the
default picks up one that's still ahead of the calendar.

Safe to re-run: it clears the competition's snapshots and builds them again from scratch, so an
interrupted run is put right by running it again. It only writes to UserCompetitionLeagueHistory -
no match, prediction or league data is touched, and nothing outside the chosen competition is.

One snapshot is written per date the competition played on. The old nightly job also left duplicate
rows on the days after a match day, which carried the same standings and drew the chart a flat step
it didn't need; those don't come back. Rows are also rebuilt from the results as they finally stand,
so a match whose result was entered a few days late now counts on the day it was played rather than
the day it was processed.
*/

--the competition to rebuild; leave NULL to take the most recently started one
DECLARE @CompetitionID UNIQUEIDENTIFIER = NULL;

IF @CompetitionID IS NULL
BEGIN
	SELECT TOP (1)
		@CompetitionID = CompetitionID
	FROM
		[dbo].[Competition]
	ORDER BY
		StartDate DESC;
END;

IF @CompetitionID IS NULL
BEGIN
	RAISERROR('There are no competitions to rebuild.', 16, 1);
	RETURN;
END;

DECLARE @CompetitionName NVARCHAR(255) = (SELECT CompetitionName FROM [dbo].[Competition] WHERE CompetitionID = @CompetitionID);

PRINT 'Rebuilding league history for: ' + @CompetitionName;

DELETE
	h
FROM
	[dbo].[UserCompetitionLeagueHistory] AS h
	INNER JOIN [dbo].[UserCompetition] AS uc ON uc.UserCompetitionID = h.UserCompetitionID
WHERE
	uc.CompetitionID = @CompetitionID;

PRINT 'Cleared ' + CAST(@@ROWCOUNT AS VARCHAR(20)) + ' existing snapshot row(s).';

DECLARE @MatchDates TABLE ([Date] DATE PRIMARY KEY);

INSERT
	@MatchDates ([Date])
SELECT DISTINCT
	CAST(m.MatchDateTime AS DATE)
FROM
	[dbo].[Match] AS m
WHERE
	m.CompetitionID = @CompetitionID
	AND m.MatchPlayed = 1;

--oldest first, because each snapshot is the standings as at its own date and
--UserCompetitionLeagueHistorySet decides whether there's anything to write by looking at what's
--already been written
DECLARE @Date DATE = (SELECT MIN([Date]) FROM @MatchDates);

WHILE @Date IS NOT NULL
BEGIN
	EXECUTE [dbo].[UserCompetitionLeagueHistorySet] @Date, @CompetitionID;

	SET @Date = (SELECT MIN([Date]) FROM @MatchDates WHERE [Date] > @Date);
END;

SELECT
	Snapshots = COUNT(*)
	, Dates = COUNT(DISTINCT h.[Date])
	, FirstDate = MIN(h.[Date])
	, LastDate = MAX(h.[Date])
FROM
	[dbo].[UserCompetitionLeagueHistory] AS h
	INNER JOIN [dbo].[UserCompetition] AS uc ON uc.UserCompetitionID = h.UserCompetitionID
WHERE
	uc.CompetitionID = @CompetitionID;
