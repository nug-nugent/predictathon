/*
Advances the sample competition by one stage, so the knockout bracket can be seen with real teams
and real scores in it rather than the wall of "Winner Group A" placeholders the seed leaves behind.

A development tool, not part of the seed. Scripts/Sample/00_RunAll.sql deliberately stops part-way
through the group stage (see the comment at the top of 04_Match.sql for why), which is right for
working on the Predictions page and useless for looking at the bracket. Run this against the Docker
dev stack to push that state forward; run it again to push it further:

    1st run  - plays out the rest of the group stage, then fills in the round of 16 from the tables
    2nd run  - plays the round of 16, then fills in the quarter-finals
    3rd run  - plays the quarter-finals, then fills in the semi-finals
    4th run  - plays the semi-finals, then fills in the final and the third-place play-off
    5th run  - plays the final and the play-off
    6th run  - reports that there is nothing left to do

Re-seeding (`.\make.ps1 clean` then `dev`, or `docker compose --env-file .env.docker up db-seed`)
puts everything back, so this is safe to experiment with.

Re-seed before running the e2e suite. Those specs are written against the state 00_RunAll.sql leaves
behind - a round of 16 full of placeholders, a particular set of open fixtures - and a competition
this script has advanced is not that state.

Each run also moves the calendar on. A match's status is worked out from its kick-off time, not from
MatchPlayed - see computeMatchStatus - so a fixture given a result while its kick-off is still in the
future comes out of this as a tie that has a score and is somehow still open for prediction. Every
match in the competition is therefore shifted back far enough to put the round just played two hours
in the past, which keeps the fixture list's shape and leaves the next round genuinely upcoming.
Prediction timestamps shift with it, by the same amount, so nothing that was entered in time becomes
a late prediction and gets invalidated by the scoring below.

Scores are derived from each match's ExternalMatchID rather than randomised, so the same run always
produces the same tournament - a screenshot taken today still matches the data tomorrow. Knockout
ties are nudged off a draw, because somebody has to go through; the site scores predictions on 90
minutes either way (the "Extra time excluded" note on the Predictions page).

Predictions are scored through dbo.MatchPredictionScoreSet, the same procedure the admin Process
Results page uses, so points come out exactly as they would had an admin entered each result by hand
- including the competition's AllowTwoPointers setting.

One caveat worth knowing. Group winners and runners-up are resolved on points, then goal difference,
then goals scored, then name. The site's own group tables can additionally separate teams level on
points by their head-to-head record (Competition.GroupHeadToHeadTieBreaks), so in a group where the
top two finish level on points, the team this script sends through may not be the one shown top of
that group. It prints a warning when that happens.
*/

-- dbo.Match carries a filtered index (IX_Match_ExternalMatchID), and SQL Server refuses to write to
-- a table with one unless these are on. Some clients - sqlcmd reading from a pipe among them - leave
-- QUOTED_IDENTIFIER off, so set both explicitly rather than depending on how the script is invoked.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

SET NOCOUNT ON;

DECLARE @CompetitionID UNIQUEIDENTIFIER = 'CA000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Competition] WHERE [CompetitionID] = @CompetitionID)
BEGIN
    RAISERROR ('Sample Cup not found - seed the sample data first (Scripts/Sample/00_RunAll.sql).', 16, 1);
    RETURN;
END

-- The matches this run completes, collected first so their predictions can be scored afterwards.
DECLARE @Played TABLE ([MatchID] UNIQUEIDENTIFIER PRIMARY KEY);

-- Which stage is next: the earliest round that still has an unplayed match. NULL rounds are the
-- group stage, and sort first because a bracket round is only reachable once the groups are done.
DECLARE @Stage VARCHAR(20) =
    CASE
        WHEN EXISTS (SELECT 1 FROM [dbo].[Match] WHERE [CompetitionID] = @CompetitionID AND [KnockoutRound] IS NULL AND [MatchPlayed] = 0) THEN 'Groups'
        WHEN EXISTS (SELECT 1 FROM [dbo].[Match] WHERE [CompetitionID] = @CompetitionID AND [KnockoutRound] IS NOT NULL AND [MatchPlayed] = 0)
            THEN 'Knockout'
        ELSE 'Complete'
    END;

IF @Stage = 'Complete'
BEGIN
    PRINT 'Nothing left to play - the sample competition has run its course. Re-seed to start again.';
    RETURN;
END

-- ---------------------------------------------------------------------------------------------
-- Play the earliest incomplete round.
--
-- The group stage goes in one go: it is a single round as far as the bracket is concerned, and
-- leaving half of it unplayed would leave the tables unable to decide who goes through. A knockout
-- round means every unplayed tie sharing the lowest KnockoutRound still outstanding - the final and
-- the third-place play-off being the one case where two "rounds" are played together.
-- ---------------------------------------------------------------------------------------------
DECLARE @Round INT = NULL;

IF @Stage = 'Knockout'
BEGIN
    -- Descending, because KnockoutRound counts teams remaining: 16 is played before 8. The play-off
    -- (3) and the final (2) are close enough together to fall out of the same rule.
    SELECT @Round = MAX([KnockoutRound])
    FROM [dbo].[Match]
    WHERE [CompetitionID] = @CompetitionID AND [KnockoutRound] IS NOT NULL AND [MatchPlayed] = 0;
END

-- ---------------------------------------------------------------------------------------------
-- Move the calendar back so the round about to be played is in the past.
--
-- Everything shifts by the same amount, matches and prediction timestamps alike: the fixture list
-- keeps its shape, the round just played reads as finished, and the round after it as upcoming.
-- "Now" is UK wall-clock, like every non-Utc datetime in this database - the container's own clock
-- runs UTC, so GETDATE() alone would be an hour out through British Summer Time.
-- ---------------------------------------------------------------------------------------------
DECLARE @UkNow DATETIME = CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'GMT Standard Time' AS DATETIME);

DECLARE @LatestKickOff DATETIME = (
    SELECT MAX([MatchDateTime])
    FROM [dbo].[Match]
    WHERE [CompetitionID] = @CompetitionID
      AND [MatchPlayed] = 0
      AND [HomeTeamID] IS NOT NULL
      AND [AwayTeamID] IS NOT NULL
      AND ((@Stage = 'Groups' AND [KnockoutRound] IS NULL)
           OR (@Stage = 'Knockout' AND [KnockoutRound] IS NOT NULL AND ([KnockoutRound] = @Round OR @Round <= 3))));

DECLARE @ShiftMinutes INT = DATEDIFF(MINUTE, @LatestKickOff, DATEADD(HOUR, -2, @UkNow));

IF @ShiftMinutes < 0
BEGIN
    UPDATE [dbo].[Match]
    SET [MatchDateTime] = DATEADD(MINUTE, @ShiftMinutes, [MatchDateTime])
    WHERE [CompetitionID] = @CompetitionID;

    -- Predictions move with the fixtures they were made against. Without this, shifting kick-offs
    -- earlier would leave every seeded prediction looking like it arrived after its match had
    -- started, and MatchPredictionScoreSet would dutifully invalidate the lot.
    UPDATE ph
    SET [PredictionDateTime] = DATEADD(MINUTE, @ShiftMinutes, ph.[PredictionDateTime])
    FROM [dbo].[PredictionHistory] AS ph
    INNER JOIN [dbo].[Prediction] AS p ON p.[PredictionID] = ph.[PredictionID]
    INNER JOIN [dbo].[Match] AS m ON m.[MatchID] = p.[MatchID]
    WHERE m.[CompetitionID] = @CompetitionID;

    PRINT CONCAT('Moved the competition back ', -@ShiftMinutes / 60, ' hour(s) so the round just played has finished.');
END

;WITH ToPlay AS (
    SELECT [MatchID], [ExternalMatchID], [KnockoutRound]
    FROM [dbo].[Match]
    WHERE [CompetitionID] = @CompetitionID
      AND [MatchPlayed] = 0
      AND [HomeTeamID] IS NOT NULL
      AND [AwayTeamID] IS NOT NULL
      AND (
            (@Stage = 'Groups' AND [KnockoutRound] IS NULL)
            -- The final and the play-off are played together: 3 and 2 are the last two values, and
            -- nothing feeds off either of them.
            OR (@Stage = 'Knockout' AND [KnockoutRound] IS NOT NULL AND ([KnockoutRound] = @Round OR @Round <= 3))
          )
)
UPDATE [dbo].[Match]
SET [MatchPlayed] = 1,
    [HomeTeamGoals] = Scores.[HomeGoals],
    [AwayTeamGoals] = Scores.[AwayGoals]
OUTPUT [inserted].[MatchID] INTO @Played ([MatchID])
FROM [dbo].[Match] AS m
INNER JOIN ToPlay ON ToPlay.[MatchID] = m.[MatchID]
CROSS APPLY (
    SELECT [Home] = (ISNULL(ToPlay.[ExternalMatchID], 1) * 7) % 4,
           [Away] = (ISNULL(ToPlay.[ExternalMatchID], 1) * 5) % 3
) AS Raw
CROSS APPLY (
    SELECT [HomeGoals] = Raw.[Home],
           -- A knockout tie can't end level: somebody has to go through, and the bracket has no way
           -- to say "won on penalties". Group matches keep their draws.
           [AwayGoals] = CASE
               WHEN ToPlay.[KnockoutRound] IS NOT NULL AND Raw.[Home] = Raw.[Away] THEN Raw.[Away] + 1
               ELSE Raw.[Away]
           END
) AS Scores;

DECLARE @PlayedCount INT = (SELECT COUNT(*) FROM @Played);

-- Score every prediction against the results just entered, exactly as Process Results would.
DECLARE @MatchID UNIQUEIDENTIFIER;
DECLARE PlayedMatches CURSOR LOCAL FAST_FORWARD FOR SELECT [MatchID] FROM @Played;
OPEN PlayedMatches;
FETCH NEXT FROM PlayedMatches INTO @MatchID;

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC [dbo].[MatchPredictionScoreSet] @MatchID = @MatchID;
    FETCH NEXT FROM PlayedMatches INTO @MatchID;
END

CLOSE PlayedMatches;
DEALLOCATE PlayedMatches;

PRINT CONCAT('Played ', @PlayedCount, ' match(es) in the ',
    CASE WHEN @Stage = 'Groups' THEN 'group stage' ELSE CONCAT('knockout round of ', @Round) END,
    ' and scored their predictions.');

-- ---------------------------------------------------------------------------------------------
-- Fill in whichever bracket slots those results have now decided.
--
-- Every undecided slot already says in words where its team comes from - "Winner Group A", "Winner
-- R16 3", "Loser SF1" - so that placeholder is what gets read here rather than a second, parallel
-- description of the draw. Anything that can't be resolved yet is simply left alone.
-- ---------------------------------------------------------------------------------------------

-- How each group finished, on points then goal difference then goals scored then name.
;WITH GroupFixtures AS (
    SELECT m.[HomeTeamID], m.[AwayTeamID], m.[HomeTeamGoals], m.[AwayTeamGoals], HomeEntry.[GroupName]
    FROM [dbo].[Match] AS m
    INNER JOIN [dbo].[TeamCompetition] AS HomeEntry
        ON HomeEntry.[TeamID] = m.[HomeTeamID] AND HomeEntry.[CompetitionID] = m.[CompetitionID]
    INNER JOIN [dbo].[TeamCompetition] AS AwayEntry
        ON AwayEntry.[TeamID] = m.[AwayTeamID] AND AwayEntry.[CompetitionID] = m.[CompetitionID]
    WHERE m.[CompetitionID] = @CompetitionID
      AND m.[KnockoutRound] IS NULL
      AND m.[MatchPlayed] = 1
      AND HomeEntry.[GroupName] IS NOT NULL
      -- Only matches inside a single group count towards that group's table.
      AND AwayEntry.[GroupName] = HomeEntry.[GroupName]
), TeamMatches AS (
    SELECT [GroupName], [TeamID] = [HomeTeamID], [For] = [HomeTeamGoals], [Against] = [AwayTeamGoals] FROM GroupFixtures
    UNION ALL
    SELECT [GroupName], [AwayTeamID], [AwayTeamGoals], [HomeTeamGoals] FROM GroupFixtures
), Totals AS (
    SELECT [GroupName], [TeamID],
        [Points] = SUM(CASE WHEN [For] > [Against] THEN 3 WHEN [For] = [Against] THEN 1 ELSE 0 END),
        [GoalDifference] = SUM([For] - [Against]),
        [GoalsFor] = SUM([For])
    FROM TeamMatches
    GROUP BY [GroupName], [TeamID]
)
SELECT
    t.[GroupName], t.[TeamID], t.[Points],
    [Position] = ROW_NUMBER() OVER (
        PARTITION BY t.[GroupName]
        ORDER BY t.[Points] DESC, t.[GoalDifference] DESC, t.[GoalsFor] DESC, team.[TeamName])
INTO #GroupStandings
FROM Totals AS t
INNER JOIN [dbo].[Team] AS team ON team.[TeamID] = t.[TeamID];

-- Warn where head-to-head could disagree - see the caveat at the top of this script.
IF EXISTS (
    SELECT 1 FROM #GroupStandings AS a
    INNER JOIN #GroupStandings AS b ON b.[GroupName] = a.[GroupName] AND b.[Position] = a.[Position] + 1
    WHERE a.[Position] IN (1, 2) AND a.[Points] = b.[Points])
BEGIN
    PRINT 'Note: at least one group has teams level on points around the qualifying places, so the';
    PRINT '      site''s head-to-head tables may order its top two differently from what went through.';
END

-- The winner of each already-played knockout tie, and its loser, keyed by the description the
-- placeholders refer to ("Round of 16 3" is what "Winner R16 3" means).
SELECT
    m.[Description],
    [WinnerTeamID] = CASE WHEN m.[HomeTeamGoals] > m.[AwayTeamGoals] THEN m.[HomeTeamID] ELSE m.[AwayTeamID] END,
    [LoserTeamID] = CASE WHEN m.[HomeTeamGoals] > m.[AwayTeamGoals] THEN m.[AwayTeamID] ELSE m.[HomeTeamID] END
INTO #TieOutcomes
FROM [dbo].[Match] AS m
WHERE m.[CompetitionID] = @CompetitionID
  AND m.[KnockoutRound] IS NOT NULL
  AND m.[MatchPlayed] = 1
  AND m.[HomeTeamID] IS NOT NULL
  AND m.[AwayTeamID] IS NOT NULL
  AND m.[Description] IS NOT NULL;

-- One row per undecided slot, with the team it resolves to. Both sides of a tie are handled by the
-- same rules, so they are unioned into one shape rather than written out twice.
;WITH Slots AS (
    SELECT [MatchID], [Side] = 'Home', [Placeholder] = [HomeTeamTBC]
    FROM [dbo].[Match]
    WHERE [CompetitionID] = @CompetitionID AND [HomeTeamID] IS NULL AND [HomeTeamTBC] IS NOT NULL
    UNION ALL
    SELECT [MatchID], 'Away', [AwayTeamTBC]
    FROM [dbo].[Match]
    WHERE [CompetitionID] = @CompetitionID AND [AwayTeamID] IS NULL AND [AwayTeamTBC] IS NOT NULL
), Resolved AS (
    -- One LEFT JOIN per shape of placeholder; each matches at most one row, and COALESCE takes
    -- whichever one did. Written as joins rather than subqueries in the projection because SQL
    -- Server will not have correlated subqueries there.
    SELECT s.[MatchID], s.[Side], s.[Placeholder],
        [TeamID] = COALESCE(GroupWinner.[TeamID], GroupRunnerUp.[TeamID],
            LastSixteen.[WinnerTeamID], QuarterFinal.[WinnerTeamID], SemiFinal.[WinnerTeamID],
            SemiFinalLoser.[LoserTeamID])
    FROM Slots AS s
    -- "Winner Group A" / "Runner-up Group A" - straight off the group table.
    LEFT JOIN #GroupStandings AS GroupWinner
        ON s.[Placeholder] LIKE 'Winner Group %'
       AND GroupWinner.[GroupName] = REPLACE(s.[Placeholder], 'Winner ', '')
       AND GroupWinner.[Position] = 1
    LEFT JOIN #GroupStandings AS GroupRunnerUp
        ON s.[Placeholder] LIKE 'Runner-up Group %'
       AND GroupRunnerUp.[GroupName] = REPLACE(s.[Placeholder], 'Runner-up ', '')
       AND GroupRunnerUp.[Position] = 2
    -- "Winner R16 3" -> the tie described as "Round of 16 3", and so on down the bracket.
    LEFT JOIN #TieOutcomes AS LastSixteen
        ON s.[Placeholder] LIKE 'Winner R16 %'
       AND LastSixteen.[Description] = REPLACE(s.[Placeholder], 'Winner R16 ', 'Round of 16 ')
    LEFT JOIN #TieOutcomes AS QuarterFinal
        ON s.[Placeholder] LIKE 'Winner QF%'
       AND QuarterFinal.[Description] = REPLACE(s.[Placeholder], 'Winner QF', 'Quarter Final ')
    LEFT JOIN #TieOutcomes AS SemiFinal
        ON s.[Placeholder] LIKE 'Winner SF%'
       AND SemiFinal.[Description] = REPLACE(s.[Placeholder], 'Winner SF', 'Semi Final ')
    -- The play-off is the one slot fed by losing rather than winning.
    LEFT JOIN #TieOutcomes AS SemiFinalLoser
        ON s.[Placeholder] LIKE 'Loser SF%'
       AND SemiFinalLoser.[Description] = REPLACE(s.[Placeholder], 'Loser SF', 'Semi Final ')
)
SELECT * INTO #ResolvedSlots FROM Resolved WHERE [TeamID] IS NOT NULL;

UPDATE m
SET [HomeTeamID] = r.[TeamID], [HomeTeamTBC] = NULL
FROM [dbo].[Match] AS m
INNER JOIN #ResolvedSlots AS r ON r.[MatchID] = m.[MatchID] AND r.[Side] = 'Home';

UPDATE m
SET [AwayTeamID] = r.[TeamID], [AwayTeamTBC] = NULL
FROM [dbo].[Match] AS m
INNER JOIN #ResolvedSlots AS r ON r.[MatchID] = m.[MatchID] AND r.[Side] = 'Away';

DECLARE @SlotsFilled INT = (SELECT COUNT(*) FROM #ResolvedSlots);
PRINT CONCAT('Filled in ', @SlotsFilled, ' bracket slot(s) from those results.');

-- The competition's own dates follow its fixtures, so the week picker and the Hall of Fame's
-- "has it finished yet" checks stay in step with the calendar that just moved.
UPDATE c
SET [StartDate] = CAST(Fixtures.[FirstKickOff] AS DATE),
    [EndDate] = CAST(Fixtures.[LastKickOff] AS DATE)
FROM [dbo].[Competition] AS c
CROSS APPLY (
    SELECT [FirstKickOff] = MIN(m.[MatchDateTime]), [LastKickOff] = MAX(m.[MatchDateTime])
    FROM [dbo].[Match] AS m WHERE m.[CompetitionID] = c.[CompetitionID]
) AS Fixtures
WHERE c.[CompetitionID] = @CompetitionID AND Fixtures.[FirstKickOff] IS NOT NULL;

DROP TABLE #ResolvedSlots;
DROP TABLE #TieOutcomes;
DROP TABLE #GroupStandings;
GO
