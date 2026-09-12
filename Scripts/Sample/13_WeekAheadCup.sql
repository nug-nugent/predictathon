/*
"Week Ahead Cup" - a competition that is deliberately NOT playing today, so the Home page's This
Week's Matches card has somewhere to appear.

That card is the one the Home page leads with on the days between matchdays: Today's Matches hides
itself when the competition has nothing on, and this takes its place. Every other competition in this
seed pins fixtures into today on purpose - Sample Cup so the live matchday reads like a real one,
Admin Cup so the admin specs have something to process - which between them means the card can never
be seen, either by hand in the Docker stack or by the e2e suite. Hence a competition of its own,
whose whole point is the empty day.

Nothing is played today and nothing is in play. Two fixtures went yesterday, so the competition has
results behind it, and the rest are spread over the following ten days - the first of them tomorrow,
which is what guarantees the card always has a week to land on and at least one row still open for
prediction, whichever day of the week the stack is brought up on.

DemoWeekAhead is registered here and nowhere else, and this is their default competition, so the spec
that owns the card just logs in - no competition switching, and nothing another spec's account can
disturb. Closed to registration and off the login page, for the same reason Admin Cup is: this is
test scaffolding, and it shouldn't turn up as another "Register for..." link on the public home page.
*/

SET NOCOUNT ON

DECLARE @WeekAheadCupID UNIQUEIDENTIFIER = 'CA000000-0000-0000-0000-000000000004';
DECLARE @DemoWeekAheadID UNIQUEIDENTIFIER = 'DB000000-0000-0000-0000-000000000004';

-- UK local wall-clock rather than GETDATE(), for the reason 04_Match.sql sets out at length: the db
-- container's OS clock runs UTC whatever the host is set to, so GETDATE() is an hour out under BST.
DECLARE @UkNow DATETIME = CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'GMT Standard Time' AS DATETIME);
DECLARE @Today DATE = CAST(@UkNow AS DATE);

MERGE INTO [dbo].[Competition] AS [Target]
USING (VALUES (
    @WeekAheadCupID,
    'Week Ahead Cup',
    0,                                          -- PrependNameWithThe
    DATEADD(DAY, -7, @Today),
    DATEADD(DAY, 21, @Today),                   -- StartDate, EndDate
    0,                                          -- DuplicateFixturesAllowed
    0,                                          -- OpenForRegistration
    0,                                          -- RegistrationAvailableOnLoginPage
    0,                                          -- ShowInHallOfFame
    0.00,                                       -- EntranceFee
    0,                                          -- PayPalPaymentAvailable
    'Scaffolding for the e2e suite: a competition with nothing on today, so the Home page shows its week-ahead card instead of Today''s Matches. See Scripts/Sample/13_WeekAheadCup.sql.',
    NULL,                                       -- ImageFilename
    1                                           -- DefaultToNeutralGround
)) AS [Source] (
    [CompetitionID], [CompetitionName], [PrependNameWithThe], [StartDate], [EndDate],
    [DuplicateFixturesAllowed], [OpenForRegistration], [RegistrationAvailableOnLoginPage],
    [ShowInHallOfFame], [EntranceFee], [PayPalPaymentAvailable], [Information], [ImageFilename],
    [DefaultToNeutralGround]
)
ON ([Target].[CompetitionID] = [Source].[CompetitionID])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([CompetitionID], [CompetitionName], [PrependNameWithThe], [StartDate], [EndDate],
        [DuplicateFixturesAllowed], [OpenForRegistration], [RegistrationAvailableOnLoginPage],
        [ShowInHallOfFame], [EntranceFee], [PayPalPaymentAvailable], [Information], [ImageFilename],
        [DefaultToNeutralGround])
    VALUES ([Source].[CompetitionID], [Source].[CompetitionName], [Source].[PrependNameWithThe],
        [Source].[StartDate], [Source].[EndDate], [Source].[DuplicateFixturesAllowed],
        [Source].[OpenForRegistration], [Source].[RegistrationAvailableOnLoginPage],
        [Source].[ShowInHallOfFame], [Source].[EntranceFee], [Source].[PayPalPaymentAvailable],
        [Source].[Information], [Source].[ImageFilename], [Source].[DefaultToNeutralGround])
-- Updates as well as inserts, the way Admin Cup's does: the dates above are relative to today, so an
-- existing Week Ahead Cup has to be pulled forward on every re-seed or its window drifts behind the
-- fixtures below.
WHEN MATCHED THEN
    UPDATE SET
        [Target].[CompetitionName] = [Source].[CompetitionName],
        [Target].[StartDate] = [Source].[StartDate],
        [Target].[EndDate] = [Source].[EndDate],
        [Target].[OpenForRegistration] = [Source].[OpenForRegistration],
        [Target].[RegistrationAvailableOnLoginPage] = [Source].[RegistrationAvailableOnLoginPage],
        [Target].[Information] = [Source].[Information];

-- Groups C and D of the World Cup draw, so this competition's teams don't overlap Admin Cup's (G and
-- H) - two specs entering predictions against a fixture that reads the same in both is exactly the
-- confusion a competition apiece is here to avoid.
MERGE INTO [dbo].[TeamCompetition] AS [Target]
USING (
    SELECT v.[TeamCompetitionID], t.[TeamID], @WeekAheadCupID AS [CompetitionID], v.[GroupName]
    FROM (VALUES
     ('7E000000-0000-0000-0000-000000000001','Argentina','Group C')
    ,('7E000000-0000-0000-0000-000000000002','Saudi Arabia','Group C')
    ,('7E000000-0000-0000-0000-000000000003','Mexico','Group C')
    ,('7E000000-0000-0000-0000-000000000004','Poland','Group C')
    ,('7E000000-0000-0000-0000-000000000005','France','Group D')
    ,('7E000000-0000-0000-0000-000000000006','Australia','Group D')
    ,('7E000000-0000-0000-0000-000000000007','Denmark','Group D')
    ,('7E000000-0000-0000-0000-000000000008','Tunisia','Group D')
    ) AS v ([TeamCompetitionID],[TeamName],[GroupName])
    INNER JOIN [dbo].[Team] AS t ON t.[TeamName] = v.[TeamName]
) AS [Source] ([TeamCompetitionID],[TeamID],[CompetitionID],[GroupName])
ON ([Target].[TeamCompetitionID] = [Source].[TeamCompetitionID])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([TeamCompetitionID],[TeamID],[CompetitionID],[GroupName])
    VALUES ([Source].[TeamCompetitionID],[Source].[TeamID],[Source].[CompetitionID],[Source].[GroupName])
WHEN MATCHED THEN
    UPDATE SET [Target].[GroupName] = [Source].[GroupName];

/*
Whole days either side of today, at ordinary kick-off times, so no arithmetic can land one of these
on today however close to midnight the stack is seeded - which is the one thing this competition must
never do.

The card picks the earliest week that still has a fixture to come (see computeUpcomingWeek), so the
two tomorrow are what pin it: a competition week runs Friday to Thursday, and the week those two fall
in is the week the card shows, whatever weekday it is today. The ones further out give it a second
week to roll on to, and yesterday's two give the competition a result and a league table rather than
a page of nothing but fixtures.
*/
DECLARE @Yesterday1 DATETIME = DATEADD(MINUTE, 900, CAST(DATEADD(DAY, -1, @Today) AS DATETIME));  -- 15:00
DECLARE @Yesterday2 DATETIME = DATEADD(MINUTE, 1185, CAST(DATEADD(DAY, -1, @Today) AS DATETIME)); -- 19:45
DECLARE @Tomorrow1 DATETIME = DATEADD(MINUTE, 900, CAST(DATEADD(DAY, 1, @Today) AS DATETIME));
DECLARE @Tomorrow2 DATETIME = DATEADD(MINUTE, 1185, CAST(DATEADD(DAY, 1, @Today) AS DATETIME));
DECLARE @In2Days DATETIME = DATEADD(MINUTE, 900, CAST(DATEADD(DAY, 2, @Today) AS DATETIME));
DECLARE @In4Days DATETIME = DATEADD(MINUTE, 1185, CAST(DATEADD(DAY, 4, @Today) AS DATETIME));
DECLARE @In8Days DATETIME = DATEADD(MINUTE, 900, CAST(DATEADD(DAY, 8, @Today) AS DATETIME));
DECLARE @In9Days DATETIME = DATEADD(MINUTE, 1185, CAST(DATEADD(DAY, 9, @Today) AS DATETIME));

MERGE INTO [dbo].[Match] AS [Target]
USING (
    SELECT
        r.[MatchID], @WeekAheadCupID AS [CompetitionID],
        CASE r.[Slot]
            WHEN 1 THEN @Yesterday1
            WHEN 2 THEN @Yesterday2
            WHEN 3 THEN @Tomorrow1
            WHEN 4 THEN @Tomorrow2
            WHEN 5 THEN @In2Days
            WHEN 6 THEN @In4Days
            WHEN 7 THEN @In8Days
            ELSE @In9Days
        END AS [MatchDateTime],
        [HomeTeam].[TeamID] AS [HomeTeamID], [AwayTeam].[TeamID] AS [AwayTeamID],
        r.[MatchPlayed], r.[HomeTeamGoals], r.[AwayTeamGoals], r.[Description]
    FROM (VALUES
     ('FC000000-0000-0000-0000-000000000001',1,'Argentina','Saudi Arabia',1,1,2,'Group C')
    ,('FC000000-0000-0000-0000-000000000002',2,'Mexico','Poland',1,0,0,'Group C')
    ,('FC000000-0000-0000-0000-000000000003',3,'France','Australia',0,NULL,NULL,'Group D')
    ,('FC000000-0000-0000-0000-000000000004',4,'Denmark','Tunisia',0,NULL,NULL,'Group D')
    ,('FC000000-0000-0000-0000-000000000005',5,'Argentina','Mexico',0,NULL,NULL,'Group C')
    ,('FC000000-0000-0000-0000-000000000006',6,'France','Denmark',0,NULL,NULL,'Group D')
    ,('FC000000-0000-0000-0000-000000000007',7,'Saudi Arabia','Poland',0,NULL,NULL,'Group C')
    ,('FC000000-0000-0000-0000-000000000008',8,'Australia','Tunisia',0,NULL,NULL,'Group D')
    ) AS r ([MatchID],[Slot],[HomeTeamName],[AwayTeamName],[MatchPlayed],[HomeTeamGoals],[AwayTeamGoals],[Description])
    INNER JOIN [dbo].[Team] AS [HomeTeam] ON [HomeTeam].[TeamName] = r.[HomeTeamName]
    INNER JOIN [dbo].[Team] AS [AwayTeam] ON [AwayTeam].[TeamName] = r.[AwayTeamName]
) AS [Source] ([MatchID],[CompetitionID],[MatchDateTime],[HomeTeamID],[AwayTeamID],[MatchPlayed],
    [HomeTeamGoals],[AwayTeamGoals],[Description])
ON ([Target].[MatchID] = [Source].[MatchID])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([MatchID],[CompetitionID],[MatchDateTime],[HomeTeamID],[AwayTeamID],[MatchPlayed],
        [HomeTeamGoals],[AwayTeamGoals],[NeutralGround],[Description],[Knockout])
    VALUES ([Source].[MatchID],[Source].[CompetitionID],[Source].[MatchDateTime],[Source].[HomeTeamID],
        [Source].[AwayTeamID],[Source].[MatchPlayed],[Source].[HomeTeamGoals],[Source].[AwayTeamGoals],
        1,[Source].[Description],0)
-- Rewritten on every seed, the way Admin Cup's fixtures are: these kick-offs are relative to today,
-- so a stack brought up a week after it was last seeded would otherwise find every one of them in
-- the past and the card with nothing left to show.
WHEN MATCHED THEN
    UPDATE SET
        [Target].[MatchDateTime] = [Source].[MatchDateTime],
        [Target].[HomeTeamID] = [Source].[HomeTeamID],
        [Target].[AwayTeamID] = [Source].[AwayTeamID],
        [Target].[MatchPlayed] = [Source].[MatchPlayed],
        [Target].[HomeTeamGoals] = [Source].[HomeTeamGoals],
        [Target].[AwayTeamGoals] = [Source].[AwayTeamGoals],
        [Target].[Description] = [Source].[Description];

MERGE INTO [dbo].[UserCompetition] AS [Target]
USING (VALUES
 ('AC000000-0000-0000-0000-000000000006', @DemoWeekAheadID, @WeekAheadCupID, 0.00, NULL, NULL, 1)
) AS [Source] ([UserCompetitionID],[UserID],[CompetitionID],[AmountPaid],[PaymentProvider],[PaymentCreditID],[IsDefaultCompetition])
ON ([Target].[UserCompetitionID] = [Source].[UserCompetitionID])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([UserCompetitionID],[UserID],[CompetitionID],[AmountPaid],[PaymentProvider],[PaymentCreditID],[IsDefaultCompetition])
    VALUES ([Source].[UserCompetitionID],[Source].[UserID],[Source].[CompetitionID],[Source].[AmountPaid],
        [Source].[PaymentProvider],[Source].[PaymentCreditID],[Source].[IsDefaultCompetition])
WHEN MATCHED THEN
    UPDATE SET [Target].[IsDefaultCompetition] = [Source].[IsDefaultCompetition];
GO

PRINT 'Week Ahead Cup seeded.';
GO
