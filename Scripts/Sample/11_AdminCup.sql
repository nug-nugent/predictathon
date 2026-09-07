/*
"Admin Cup" - a competition that exists so the e2e suite's admin specs have match state of their own
to change.

process-results.spec confirms a result; live-score.spec puts a live score in. Both are destructive in
a way no player spec is - confirming a result takes a match out of play for good, and live.spec
asserts that matches ARE in play. Pointed at the same competition those fight, and the suite fails a
different spec on each run depending on which got there first.

Nothing in the tests has to know this exists. Every match route in the API is competition-scoped
(MatchService.GetForProcessingAsync filters on CompetitionID, and the controller takes it from the
route), and the frontend never asks a test which competition it means - CompetitionProvider resolves
it from the signed-in user's own registrations, preferring the one flagged IsDefaultCompetition. So
making this DemoAdmin's default is on its own enough to keep the admin specs off Sample Cup.

DemoAdmin stays registered in Sample Cup too, just not as the default, so you can still switch to it
by hand to look at the sample data through an admin's eyes.

Closed to registration and off the login page: this is test scaffolding, and it would otherwise show
up as a third "Register for..." link on the public home page and change what no-competitions.spec
sees there.
*/

SET NOCOUNT ON

DECLARE @AdminCupID UNIQUEIDENTIFIER = 'CA000000-0000-0000-0000-000000000003';
DECLARE @DemoAdminID UNIQUEIDENTIFIER = 'CC44CE3F-CC89-44E9-98E1-08DEE1D3E748';
DECLARE @SampleCupID UNIQUEIDENTIFIER = 'CA000000-0000-0000-0000-000000000001';

-- UK local wall-clock rather than GETDATE(), for the reason 04_Match.sql sets out at length: the db
-- container's OS clock runs UTC whatever the host is set to, so GETDATE() is an hour out under BST.
DECLARE @UkNow DATETIME = CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'GMT Standard Time' AS DATETIME);
DECLARE @StartOfToday DATETIME = CAST(CAST(@UkNow AS DATE) AS DATETIME);
DECLARE @EndOfToday DATETIME = DATEADD(MINUTE, -5, DATEADD(DAY, 1, @StartOfToday));

MERGE INTO [dbo].[Competition] AS [Target]
USING (VALUES (
    @AdminCupID,
    'Admin Cup',
    0,                                          -- PrependNameWithThe
    DATEADD(DAY, -7, CAST(@UkNow AS DATE)),
    DATEADD(DAY, 7, CAST(@UkNow AS DATE)),      -- StartDate, EndDate
    0,                                          -- DuplicateFixturesAllowed
    0,                                          -- OpenForRegistration
    0,                                          -- RegistrationAvailableOnLoginPage
    0,                                          -- ShowInHallOfFame
    0.00,                                       -- EntranceFee
    0,                                          -- PayPalPaymentAvailable
    'Scaffolding for the e2e suite: the competition its admin specs enter results and live scores against, so they cannot disturb the fixtures the player specs are reading. See Scripts/Sample/11_AdminCup.sql.',
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
-- Unlike 02_Competition.sql's MERGE this updates as well as inserts: the dates above are relative to
-- today, so an existing Admin Cup has to be pulled forward on every re-seed or its window drifts
-- behind the matches below.
WHEN MATCHED THEN
    UPDATE SET
        [Target].[CompetitionName] = [Source].[CompetitionName],
        [Target].[StartDate] = [Source].[StartDate],
        [Target].[EndDate] = [Source].[EndDate],
        [Target].[OpenForRegistration] = [Source].[OpenForRegistration],
        [Target].[RegistrationAvailableOnLoginPage] = [Source].[RegistrationAvailableOnLoginPage],
        [Target].[Information] = [Source].[Information];

/*
Eight teams and eight fixtures, all pinned into today so they land on the Home page's card, and all
clamped inside today's own date the way 04_Match.sql clamps its own - a stack seeded just after
midnight would otherwise push "six hours ago" onto yesterday, where a confirmed result drops out of
the day's window entirely.

The shape is chosen for what the two admin specs need:

  Three in play and past MatchService's 90-minute rule, so the Process Results page has something to
    offer. Three rather than one so the suite can be run twice over without a re-seed - each run
    confirms one, and it stays confirmed until the MERGE below resets it.
  One in play but only 35 minutes in, too recent to process, so there is always a live match left for
    live-score.spec to put a score against however many results have been confirmed.
  Two finished earlier and two still to come, so the admin's own Home page reads like a real day
    rather than a page of nothing but in-play rows.
*/
DECLARE @Finished1 DATETIME = CASE
    WHEN DATEADD(HOUR, -6, @UkNow) < @StartOfToday THEN DATEADD(MINUTE, 1, @StartOfToday)
    ELSE DATEADD(HOUR, -6, @UkNow) END;
DECLARE @Finished2 DATETIME = CASE
    WHEN DATEADD(HOUR, -4, @UkNow) < @StartOfToday THEN DATEADD(MINUTE, 2, @StartOfToday)
    ELSE DATEADD(HOUR, -4, @UkNow) END;
DECLARE @Processable1 DATETIME = CASE
    WHEN DATEADD(MINUTE, -110, @UkNow) < @StartOfToday THEN DATEADD(MINUTE, 3, @StartOfToday)
    ELSE DATEADD(MINUTE, -110, @UkNow) END;
DECLARE @Processable2 DATETIME = CASE
    WHEN DATEADD(MINUTE, -100, @UkNow) < @StartOfToday THEN DATEADD(MINUTE, 4, @StartOfToday)
    ELSE DATEADD(MINUTE, -100, @UkNow) END;
DECLARE @Processable3 DATETIME = CASE
    WHEN DATEADD(MINUTE, -95, @UkNow) < @StartOfToday THEN DATEADD(MINUTE, 5, @StartOfToday)
    ELSE DATEADD(MINUTE, -95, @UkNow) END;
DECLARE @TooRecent DATETIME = CASE
    WHEN DATEADD(MINUTE, -35, @UkNow) < @StartOfToday THEN DATEADD(MINUTE, 6, @StartOfToday)
    ELSE DATEADD(MINUTE, -35, @UkNow) END;
DECLARE @ComingUp1 DATETIME = CASE
    WHEN DATEADD(MINUTE, 45, @UkNow) > @EndOfToday THEN DATEADD(MINUTE, -2, @EndOfToday)
    ELSE DATEADD(MINUTE, 45, @UkNow) END;
DECLARE @ComingUp2 DATETIME = CASE
    WHEN DATEADD(HOUR, 3, @UkNow) > @EndOfToday THEN DATEADD(MINUTE, -1, @EndOfToday)
    ELSE DATEADD(HOUR, 3, @UkNow) END;

MERGE INTO [dbo].[TeamCompetition] AS [Target]
USING (
    SELECT v.[TeamCompetitionID], t.[TeamID], @AdminCupID AS [CompetitionID], v.[GroupName]
    FROM (VALUES
     ('7D000000-0000-0000-0000-000000000001','Brazil','Group A')
    ,('7D000000-0000-0000-0000-000000000002','Serbia','Group A')
    ,('7D000000-0000-0000-0000-000000000003','Switzerland','Group A')
    ,('7D000000-0000-0000-0000-000000000004','Cameroon','Group A')
    ,('7D000000-0000-0000-0000-000000000005','Portugal','Group B')
    ,('7D000000-0000-0000-0000-000000000006','Ghana','Group B')
    ,('7D000000-0000-0000-0000-000000000007','Uruguay','Group B')
    ,('7D000000-0000-0000-0000-000000000008','South Korea','Group B')
    ) AS v ([TeamCompetitionID],[TeamName],[GroupName])
    INNER JOIN [dbo].[Team] AS t ON t.[TeamName] = v.[TeamName]
) AS [Source] ([TeamCompetitionID],[TeamID],[CompetitionID],[GroupName])
ON ([Target].[TeamCompetitionID] = [Source].[TeamCompetitionID])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([TeamCompetitionID],[TeamID],[CompetitionID],[GroupName])
    VALUES ([Source].[TeamCompetitionID],[Source].[TeamID],[Source].[CompetitionID],[Source].[GroupName])
WHEN MATCHED THEN
    UPDATE SET [Target].[GroupName] = [Source].[GroupName];

MERGE INTO [dbo].[Match] AS [Target]
USING (
    SELECT
        r.[MatchID], @AdminCupID AS [CompetitionID],
        CASE r.[Slot]
            WHEN 1 THEN @Finished1
            WHEN 2 THEN @Finished2
            WHEN 3 THEN @Processable1
            WHEN 4 THEN @Processable2
            WHEN 5 THEN @Processable3
            WHEN 6 THEN @TooRecent
            WHEN 7 THEN @ComingUp1
            ELSE @ComingUp2
        END AS [MatchDateTime],
        [HomeTeam].[TeamID] AS [HomeTeamID], [AwayTeam].[TeamID] AS [AwayTeamID],
        r.[MatchPlayed], r.[HomeTeamGoals], r.[AwayTeamGoals], r.[Description]
    FROM (VALUES
     ('FB000000-0000-0000-0000-000000000001',1,'Brazil','Serbia',1,2,1,'Group A')
    ,('FB000000-0000-0000-0000-000000000002',2,'Switzerland','Cameroon',1,0,0,'Group A')
    ,('FB000000-0000-0000-0000-000000000003',3,'Portugal','Ghana',0,NULL,NULL,'Group B')
    ,('FB000000-0000-0000-0000-000000000004',4,'Uruguay','South Korea',0,NULL,NULL,'Group B')
    ,('FB000000-0000-0000-0000-000000000005',5,'Brazil','Switzerland',0,NULL,NULL,'Group A')
    ,('FB000000-0000-0000-0000-000000000006',6,'Serbia','Cameroon',0,NULL,NULL,'Group A')
    ,('FB000000-0000-0000-0000-000000000007',7,'Portugal','Uruguay',0,NULL,NULL,'Group B')
    ,('FB000000-0000-0000-0000-000000000008',8,'Ghana','South Korea',0,NULL,NULL,'Group B')
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
-- This UPDATE is the whole reason Admin Cup heals itself where Sample Cup's predictions cannot: every
-- change the admin specs make is to a match row, and every match row is rewritten here on each seed.
WHEN MATCHED THEN
    UPDATE SET
        [Target].[MatchDateTime] = [Source].[MatchDateTime],
        [Target].[HomeTeamID] = [Source].[HomeTeamID],
        [Target].[AwayTeamID] = [Source].[AwayTeamID],
        [Target].[MatchPlayed] = [Source].[MatchPlayed],
        [Target].[HomeTeamGoals] = [Source].[HomeTeamGoals],
        [Target].[AwayTeamGoals] = [Source].[AwayTeamGoals],
        [Target].[Description] = [Source].[Description];

-- Any live score left over from a previous run of live-score.spec. The match itself is reset above,
-- so leaving the score behind would show yesterday's 4 - 2 against a fixture yet to kick off.
DELETE s
FROM [dbo].[MatchLiveScore] AS s
INNER JOIN [dbo].[Match] AS m ON m.[MatchID] = s.[MatchID]
WHERE m.[CompetitionID] = @AdminCupID;

MERGE INTO [dbo].[UserCompetition] AS [Target]
USING (VALUES
 ('AC000000-0000-0000-0000-000000000005', @DemoAdminID, @AdminCupID, 0.00, NULL, NULL, 1)
) AS [Source] ([UserCompetitionID],[UserID],[CompetitionID],[AmountPaid],[PaymentProvider],[PaymentCreditID],[IsDefaultCompetition])
ON ([Target].[UserCompetitionID] = [Source].[UserCompetitionID])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([UserCompetitionID],[UserID],[CompetitionID],[AmountPaid],[PaymentProvider],[PaymentCreditID],[IsDefaultCompetition])
    VALUES ([Source].[UserCompetitionID],[Source].[UserID],[Source].[CompetitionID],[Source].[AmountPaid],
        [Source].[PaymentProvider],[Source].[PaymentCreditID],[Source].[IsDefaultCompetition])
WHEN MATCHED THEN
    UPDATE SET [Target].[IsDefaultCompetition] = [Source].[IsDefaultCompetition];

-- The other half of that: 07_UserCompetition.sql registers DemoAdmin in Sample Cup as their default,
-- because at that point it is their only competition. Demote it here rather than there, so 07 keeps
-- describing the plain two-account registration and this file owns the whole of the redirection.
UPDATE [dbo].[UserCompetition]
SET [IsDefaultCompetition] = 0
WHERE [UserID] = @DemoAdminID AND [CompetitionID] = @SampleCupID;
GO

PRINT 'Admin Cup seeded.';
GO
