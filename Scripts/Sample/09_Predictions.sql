/*
Predictions for every Sample Cup competitor, so the standings mean something.

Until this ran, nobody had predicted anything: the league table showed every player on nought
points with 56 missed predictions, the Statistics and Hall of Fame pages had nothing to rank, and
the Live page's projected positions moved everyone at once because the table was one big tie.

Two sets are seeded, and deliberately no more:

  - The 56 already-played matches, so there are standings to stand in. These are generated from the
    real result so the spread is believable rather than random noise - roughly a fifth are spot on,
    a third get the result right, and one in ten didn't predict at all. Scores are then computed by
    MatchPredictionScoreSet, the same procedure the app uses when an admin enters a result, so the
    points here are exactly the points the app would award.

  - The matches already in play at seed time, so the Live page's table has gains to show. These
    can't be correlated with anything - the live score doesn't exist until the poller first runs -
    so they're just plausible scorelines.

Matches still to kick off are left unpredicted on purpose: the Predictions page, the Home page's
deadline card and the e2e prediction test all need something outstanding to act on.
*/

SET NOCOUNT ON

DECLARE @SampleCupID UNIQUEIDENTIFIER = 'CA000000-0000-0000-0000-000000000001';
DECLARE @UkNow DATETIME = CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'GMT Standard Time' AS DATETIME);

-- A stable number per (user, match) pair: the same competitor always makes the same prediction for
-- the same fixture, so re-seeding doesn't reshuffle the league table.
;WITH Pairing AS (
    SELECT
        m.MatchID
        , m.MatchPlayed
        , ActualHome = m.HomeTeamGoals
        , ActualAway = m.AwayTeamGoals
        , UserID = uc.UserID
        , Bucket = ABS(CHECKSUM(CAST(uc.UserID AS VARCHAR(36)) + '|' + CAST(m.MatchID AS VARCHAR(36)))) % 10
    FROM
        [dbo].[Match] AS m
        INNER JOIN [dbo].[UserCompetition] AS uc ON uc.CompetitionID = m.CompetitionID
    WHERE
        m.CompetitionID = @SampleCupID
        -- Played, or kicked off and still awaiting a result. Anything still to come is left alone.
        AND (m.MatchPlayed = 1 OR m.MatchDateTime <= @UkNow)
),
Predicted AS (
    SELECT
        MatchID
        , UserID
        , PredictedHome = CASE
            WHEN MatchPlayed = 0 THEN Bucket % 4
            -- Bang on.
            WHEN Bucket <= 1 THEN ActualHome
            -- Right result, and the winner's goals right too - a 2-pointer where the competition
            -- allows them. A draw has no winner to get right, so it takes the near-miss below.
            WHEN Bucket <= 3 AND ActualHome > ActualAway THEN ActualHome
            WHEN Bucket <= 3 AND ActualAway > ActualHome THEN ActualHome + 1
            -- Right result, wrong scoreline.
            WHEN Bucket <= 5 THEN ActualHome + 1
            -- Wrong result: the scoreline reversed, which turns a win into a defeat. A drawn match
            -- reversed is still a draw, so those get a home win instead.
            WHEN ActualHome = ActualAway THEN ActualHome + 1
            ELSE ActualAway
          END
        , PredictedAway = CASE
            WHEN MatchPlayed = 0 THEN (Bucket / 2) % 4
            WHEN Bucket <= 1 THEN ActualAway
            WHEN Bucket <= 3 AND ActualHome > ActualAway THEN ActualAway + 1
            WHEN Bucket <= 3 AND ActualAway > ActualHome THEN ActualAway
            WHEN Bucket <= 5 THEN ActualAway + 1
            WHEN ActualHome = ActualAway THEN ActualAway
            ELSE ActualHome
          END
    FROM
        Pairing
    WHERE
        -- One competitor in ten simply didn't get round to it, which is what makes the league
        -- table's "no prediction" column worth having.
        Bucket <> 9
)
INSERT INTO [dbo].[Prediction] ([PredictionID],[MatchID],[UserID],[HomeTeamGoals],[AwayTeamGoals])
SELECT
    NEWID()
    , p.MatchID
    , p.UserID
    , p.PredictedHome
    , p.PredictedAway
FROM
    Predicted AS p
WHERE
    NOT EXISTS (SELECT 1 FROM [dbo].[Prediction] AS existing WHERE existing.MatchID = p.MatchID AND existing.UserID = p.UserID);
GO

/*
Score the played matches through the real scoring procedure rather than working the points out here.
The rules live in one place (MatchPredictionScoreSet) and a seed script quietly implementing its own
copy of them would be a second definition to keep in step - and would happily seed a league table
the app itself disagrees with.
*/
DECLARE @MatchID UNIQUEIDENTIFIER;

DECLARE PlayedMatches CURSOR LOCAL FAST_FORWARD FOR
    SELECT MatchID FROM [dbo].[Match] WHERE CompetitionID = 'CA000000-0000-0000-0000-000000000001' AND MatchPlayed = 1;

OPEN PlayedMatches;
FETCH NEXT FROM PlayedMatches INTO @MatchID;

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC [dbo].[MatchPredictionScoreSet] @MatchID = @MatchID;
    FETCH NEXT FROM PlayedMatches INTO @MatchID;
END;

CLOSE PlayedMatches;
DEALLOCATE PlayedMatches;
GO
