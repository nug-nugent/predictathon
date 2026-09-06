/*
64 matches for "Sample Cup" - full 32-team World Cup format (8 groups of 4, round-robin group stage,
then Round of 16, Quarter-finals, Semi-finals, 3rd Place Play-off, Final). Scores are
deterministically generated (not random), and the schedule sits part-way through the group stage:
matchdays 1 and 2 are complete (MatchPlayed = 1), and matchday 3 is in progress - its first day is
today, its second tomorrow. Everything from the Round of 16 on uses HomeTeamTBC/AwayTeamTBC
placeholders ('Winner Group A', 'Winner R16 1'), since with the groups unfinished those teams aren't
decided yet - the same convention Scripts/WorldCup2026FixtureImport.sql uses for unresolved fixtures.

Stopping here rather than nearer the end is deliberate. The earlier arrangement had 56 of the 64
matches played, which left only eight open for prediction - four of them TBC placeholders - so the
Predictions page had barely two rows to act on, parallel e2e specs trod on each other's fixtures,
and the single "coming up" fixture aged out half an hour after seeding. Stopping mid-group leaves
around thirty open matches, spread over the following fortnight, and the ones people actually
interact with are group fixtures with real teams rather than placeholders.

The groups are interleaved into three proper matchdays rather than played out one group at a time,
which is both how a World Cup actually runs and what makes "matchdays 1 and 2 played" meaningful:
every one of the 32 teams then has two results and one fixture still to come, wherever you cut. Each
group's six matches are listed as a sequential round-robin (T1vT2, T1vT3, T1vT4, T2vT3, T2vT4,
T3vT4), so the matchdays pair them up as MD1 = {T1vT2, T3vT4}, MD2 = {T1vT3, T2vT4},
MD3 = {T1vT4, T2vT3} - no team playing twice in a day.

Column order: MatchID, CompetitionID, MatchDateTime, HomeTeamID, AwayTeamID, MatchPlayed,
HomeTeamGoals, AwayTeamGoals, NeutralGround, HomeTeamTBC, AwayTeamTBC, Description, Knockout.
*/

SET NOCOUNT ON

-- Match dates below are the days this script was first written against (group matchday 3 opening on
-- 2026-07-09), not literal target dates - hardcoding them would mean the "part-way through" story
-- silently drifts into the past the longer this seed script goes unrun, eventually leaving no match
-- open for prediction at all. Every date is shifted by the same number of days so the schedule keeps
-- its shape, anchored so that matchday 3's first day lands on today.
--
-- "Now" is computed as UK local time rather than GETDATE() - the app has no timezone handling of its
-- own (everything's just naive local wall-clock times, fine for a single-region hobby app), and the
-- Docker db container's OS clock runs UTC regardless of the host's timezone, so GETDATE() alone
-- would be off by an hour during British Summer Time.
DECLARE @UkNow DATETIME = CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'GMT Standard Time' AS DATETIME);
DECLARE @StartOfToday DATETIME = CAST(CAST(@UkNow AS DATE) AS DATETIME);
DECLARE @EndOfToday DATETIME = DATEADD(MINUTE, -5, DATEADD(DAY, 1, @StartOfToday));
DECLARE @DateShiftDays INT = DATEDIFF(DAY, '2026-07-09', @UkNow);

-- Matchday 3 for Groups A-D is "today", and all eight of its matches are pinned to today's clock
-- rather than shifted by whole days, so the Home page's card has something in each of its three
-- groups the moment the stack comes up - and keeps having one for hours rather than minutes:
--
--   Two completed earlier today, so "Completed" has rows and the day has results to read.
--   Three in play. Two of them kicked off far enough back (110 and 95 minutes) that the Process
--     Results page will accept a result for them - MatchService's 90-minute rule, which the e2e
--     process-results spec drives. Deliberately two rather than one: that spec confirms a result
--     and so consumes one, and live.spec.ts needs more than a single match still in play. The
--     third is 35 minutes in - genuinely mid-match, and too recent to process.
--   Three still to come, from 30 minutes out to 5 hours. The half-hour one covers the near-deadline
--     states (and the e2e quick-predict spec); the rest keep "Coming up" populated across a working
--     session rather than the half hour a single pinned fixture used to give.
--
-- Each is clamped into today's own date so a stack seeded just after midnight or late in the evening
-- still gets all eight onto today's card instead of spilling onto yesterday or tomorrow - where a
-- confirmed result drops out of the day's window entirely (see LiveDayWindow). The minute steps keep
-- clamped fixtures on distinct kick-offs rather than collapsing them onto one. Seeding inside the
-- first few minutes after midnight is the one case where a clamped "live" match can land a minute or
-- two ahead of "now"; it corrects itself as the clock moves on.
DECLARE @CompletedEarlier1 DATETIME = CASE
    WHEN DATEADD(HOUR, -6, @UkNow) < @StartOfToday THEN DATEADD(MINUTE, 1, @StartOfToday)
    ELSE DATEADD(HOUR, -6, @UkNow) END;
DECLARE @CompletedEarlier2 DATETIME = CASE
    WHEN DATEADD(HOUR, -4, @UkNow) < @StartOfToday THEN DATEADD(MINUTE, 2, @StartOfToday)
    ELSE DATEADD(HOUR, -4, @UkNow) END;
DECLARE @InPlayProcessable1 DATETIME = CASE
    WHEN DATEADD(MINUTE, -110, @UkNow) < @StartOfToday THEN DATEADD(MINUTE, 3, @StartOfToday)
    ELSE DATEADD(MINUTE, -110, @UkNow) END;
DECLARE @InPlayProcessable2 DATETIME = CASE
    WHEN DATEADD(MINUTE, -95, @UkNow) < @StartOfToday THEN DATEADD(MINUTE, 4, @StartOfToday)
    ELSE DATEADD(MINUTE, -95, @UkNow) END;
DECLARE @InPlayTooRecent DATETIME = CASE
    WHEN DATEADD(MINUTE, -35, @UkNow) < @StartOfToday THEN DATEADD(MINUTE, 5, @StartOfToday)
    ELSE DATEADD(MINUTE, -35, @UkNow) END;
DECLARE @ComingUp1 DATETIME = CASE
    WHEN DATEADD(MINUTE, 30, @UkNow) > @EndOfToday THEN DATEADD(MINUTE, -3, @EndOfToday)
    ELSE DATEADD(MINUTE, 30, @UkNow) END;
DECLARE @ComingUp2 DATETIME = CASE
    WHEN DATEADD(HOUR, 2, @UkNow) > @EndOfToday THEN DATEADD(MINUTE, -2, @EndOfToday)
    ELSE DATEADD(HOUR, 2, @UkNow) END;
DECLARE @ComingUp3 DATETIME = CASE
    WHEN DATEADD(HOUR, 5, @UkNow) > @EndOfToday THEN DATEADD(MINUTE, -1, @EndOfToday)
    ELSE DATEADD(HOUR, 5, @UkNow) END;

MERGE INTO [dbo].[Match] AS [Target]
USING (
    SELECT [MatchID],[CompetitionID],
        CASE WHEN [MatchID] = 'FA000000-0000-0000-0000-000000000003' THEN @CompletedEarlier1
             WHEN [MatchID] = 'FA000000-0000-0000-0000-000000000009' THEN @CompletedEarlier2
             WHEN [MatchID] = 'FA000000-0000-0000-0000-000000000004' THEN @InPlayProcessable1
             WHEN [MatchID] = 'FA000000-0000-0000-0000-000000000010' THEN @InPlayProcessable2
             WHEN [MatchID] = 'FA000000-0000-0000-0000-000000000015' THEN @InPlayTooRecent
             WHEN [MatchID] = 'FA000000-0000-0000-0000-000000000016' THEN @ComingUp1
             WHEN [MatchID] = 'FA000000-0000-0000-0000-000000000021' THEN @ComingUp2
             WHEN [MatchID] = 'FA000000-0000-0000-0000-000000000022' THEN @ComingUp3
             ELSE DATEADD(DAY, @DateShiftDays, [MatchDateTime]) END AS [MatchDateTime],
        [HomeTeam].[TeamID] AS [HomeTeamID],[AwayTeam].[TeamID] AS [AwayTeamID],
        [MatchPlayed],[HomeTeamGoals],[AwayTeamGoals],[NeutralGround],
        [HomeTeamTBC],[AwayTeamTBC],[Description],[Knockout]
    FROM (VALUES
('FA000000-0000-0000-0000-000000000001','CA000000-0000-0000-0000-000000000001','2026-07-03 12:00:00','335DF488-AB0A-4845-A1F1-90E11DC61B39','A11C05D2-68BE-4BE8-B236-FEEFF79EA173',1,0,0,1,NULL,NULL,'Group A',0)
,('FA000000-0000-0000-0000-000000000002','CA000000-0000-0000-0000-000000000001','2026-07-06 12:00:00','335DF488-AB0A-4845-A1F1-90E11DC61B39','072265B7-4B46-4DB4-A6BA-E26968CB3528',1,0,0,1,NULL,NULL,'Group A',0)
,('FA000000-0000-0000-0000-000000000003','CA000000-0000-0000-0000-000000000001','2026-07-09 12:00:00','335DF488-AB0A-4845-A1F1-90E11DC61B39','1888BEC3-88D5-46D3-8F22-BA16598F92E0',1,0,0,1,NULL,NULL,'Group A',0)
,('FA000000-0000-0000-0000-000000000004','CA000000-0000-0000-0000-000000000001','2026-07-09 12:00:00','A11C05D2-68BE-4BE8-B236-FEEFF79EA173','072265B7-4B46-4DB4-A6BA-E26968CB3528',0,NULL,NULL,1,NULL,NULL,'Group A',0)
,('FA000000-0000-0000-0000-000000000005','CA000000-0000-0000-0000-000000000001','2026-07-06 12:00:00','A11C05D2-68BE-4BE8-B236-FEEFF79EA173','1888BEC3-88D5-46D3-8F22-BA16598F92E0',1,0,0,1,NULL,NULL,'Group A',0)
,('FA000000-0000-0000-0000-000000000006','CA000000-0000-0000-0000-000000000001','2026-07-03 12:00:00','072265B7-4B46-4DB4-A6BA-E26968CB3528','1888BEC3-88D5-46D3-8F22-BA16598F92E0',1,0,0,1,NULL,NULL,'Group A',0)
,('FA000000-0000-0000-0000-000000000007','CA000000-0000-0000-0000-000000000001','2026-07-03 12:00:00','8DC22F7B-26DC-4A1C-9339-EFCCE7FC3DB7','69BB73A4-04C0-4498-BD7D-913ABF5013E1',1,0,1,1,NULL,NULL,'Group B',0)
,('FA000000-0000-0000-0000-000000000008','CA000000-0000-0000-0000-000000000001','2026-07-06 12:00:00','8DC22F7B-26DC-4A1C-9339-EFCCE7FC3DB7','26864295-E023-48DA-B99F-5E986BBAD66B',1,0,2,1,NULL,NULL,'Group B',0)
,('FA000000-0000-0000-0000-000000000009','CA000000-0000-0000-0000-000000000001','2026-07-09 12:00:00','8DC22F7B-26DC-4A1C-9339-EFCCE7FC3DB7','831DC56C-61CD-4E0F-89C7-61AAFCB42FC4',1,0,0,1,NULL,NULL,'Group B',0)
,('FA000000-0000-0000-0000-000000000010','CA000000-0000-0000-0000-000000000001','2026-07-09 12:00:00','69BB73A4-04C0-4498-BD7D-913ABF5013E1','26864295-E023-48DA-B99F-5E986BBAD66B',0,NULL,NULL,1,NULL,NULL,'Group B',0)
,('FA000000-0000-0000-0000-000000000011','CA000000-0000-0000-0000-000000000001','2026-07-06 12:00:00','69BB73A4-04C0-4498-BD7D-913ABF5013E1','831DC56C-61CD-4E0F-89C7-61AAFCB42FC4',1,1,0,1,NULL,NULL,'Group B',0)
,('FA000000-0000-0000-0000-000000000012','CA000000-0000-0000-0000-000000000001','2026-07-03 12:00:00','26864295-E023-48DA-B99F-5E986BBAD66B','831DC56C-61CD-4E0F-89C7-61AAFCB42FC4',1,2,0,1,NULL,NULL,'Group B',0)
,('FA000000-0000-0000-0000-000000000013','CA000000-0000-0000-0000-000000000001','2026-07-03 17:00:00','53C0B07F-46E9-4311-A32E-DCEBDF06D2C3','6A05141D-32D9-4D10-964C-1C5826F36793',1,0,0,1,NULL,NULL,'Group C',0)
,('FA000000-0000-0000-0000-000000000014','CA000000-0000-0000-0000-000000000001','2026-07-06 17:00:00','53C0B07F-46E9-4311-A32E-DCEBDF06D2C3','F386071E-E336-4468-9449-62BB56F854C9',1,0,0,1,NULL,NULL,'Group C',0)
,('FA000000-0000-0000-0000-000000000015','CA000000-0000-0000-0000-000000000001','2026-07-09 17:00:00','53C0B07F-46E9-4311-A32E-DCEBDF06D2C3','729CA748-2DF6-4238-B641-C835694EB596',0,NULL,NULL,1,NULL,NULL,'Group C',0)
,('FA000000-0000-0000-0000-000000000016','CA000000-0000-0000-0000-000000000001','2026-07-09 17:00:00','6A05141D-32D9-4D10-964C-1C5826F36793','F386071E-E336-4468-9449-62BB56F854C9',0,NULL,NULL,1,NULL,NULL,'Group C',0)
,('FA000000-0000-0000-0000-000000000017','CA000000-0000-0000-0000-000000000001','2026-07-06 17:00:00','6A05141D-32D9-4D10-964C-1C5826F36793','729CA748-2DF6-4238-B641-C835694EB596',1,0,0,1,NULL,NULL,'Group C',0)
,('FA000000-0000-0000-0000-000000000018','CA000000-0000-0000-0000-000000000001','2026-07-03 17:00:00','F386071E-E336-4468-9449-62BB56F854C9','729CA748-2DF6-4238-B641-C835694EB596',1,0,0,1,NULL,NULL,'Group C',0)
,('FA000000-0000-0000-0000-000000000019','CA000000-0000-0000-0000-000000000001','2026-07-03 17:00:00','B82228E0-1A32-423D-A93D-EC163B6E97FE','3586E607-7CD5-469C-BDD5-B88CCA05E033',1,0,0,1,NULL,NULL,'Group D',0)
,('FA000000-0000-0000-0000-000000000020','CA000000-0000-0000-0000-000000000001','2026-07-06 17:00:00','B82228E0-1A32-423D-A93D-EC163B6E97FE','AB3581B7-F134-49EB-AEC1-2B6295BFD238',1,0,0,1,NULL,NULL,'Group D',0)
,('FA000000-0000-0000-0000-000000000021','CA000000-0000-0000-0000-000000000001','2026-07-09 17:00:00','B82228E0-1A32-423D-A93D-EC163B6E97FE','E3D0873E-1821-41E5-9C8D-67683029531B',0,NULL,NULL,1,NULL,NULL,'Group D',0)
,('FA000000-0000-0000-0000-000000000022','CA000000-0000-0000-0000-000000000001','2026-07-09 17:00:00','3586E607-7CD5-469C-BDD5-B88CCA05E033','AB3581B7-F134-49EB-AEC1-2B6295BFD238',0,NULL,NULL,1,NULL,NULL,'Group D',0)
,('FA000000-0000-0000-0000-000000000023','CA000000-0000-0000-0000-000000000001','2026-07-06 17:00:00','3586E607-7CD5-469C-BDD5-B88CCA05E033','E3D0873E-1821-41E5-9C8D-67683029531B',1,0,0,1,NULL,NULL,'Group D',0)
,('FA000000-0000-0000-0000-000000000024','CA000000-0000-0000-0000-000000000001','2026-07-03 17:00:00','AB3581B7-F134-49EB-AEC1-2B6295BFD238','E3D0873E-1821-41E5-9C8D-67683029531B',1,0,0,1,NULL,NULL,'Group D',0)
,('FA000000-0000-0000-0000-000000000025','CA000000-0000-0000-0000-000000000001','2026-07-04 12:00:00','6AD75FB4-D0F4-4D14-9949-DBC8ED161543','B88E7FBD-C239-48FF-BEF7-96FA6EDCD0D8',1,0,0,1,NULL,NULL,'Group E',0)
,('FA000000-0000-0000-0000-000000000026','CA000000-0000-0000-0000-000000000001','2026-07-07 12:00:00','6AD75FB4-D0F4-4D14-9949-DBC8ED161543','F79B9FA3-FB0B-4ECC-8074-25F47400DB33',1,0,0,1,NULL,NULL,'Group E',0)
,('FA000000-0000-0000-0000-000000000027','CA000000-0000-0000-0000-000000000001','2026-07-10 12:00:00','6AD75FB4-D0F4-4D14-9949-DBC8ED161543','0D6C9C86-0754-490F-990C-A1E5C21C1FFC',0,NULL,NULL,1,NULL,NULL,'Group E',0)
,('FA000000-0000-0000-0000-000000000028','CA000000-0000-0000-0000-000000000001','2026-07-10 12:00:00','B88E7FBD-C239-48FF-BEF7-96FA6EDCD0D8','F79B9FA3-FB0B-4ECC-8074-25F47400DB33',0,NULL,NULL,1,NULL,NULL,'Group E',0)
,('FA000000-0000-0000-0000-000000000029','CA000000-0000-0000-0000-000000000001','2026-07-07 12:00:00','B88E7FBD-C239-48FF-BEF7-96FA6EDCD0D8','0D6C9C86-0754-490F-990C-A1E5C21C1FFC',1,0,0,1,NULL,NULL,'Group E',0)
,('FA000000-0000-0000-0000-000000000030','CA000000-0000-0000-0000-000000000001','2026-07-04 12:00:00','F79B9FA3-FB0B-4ECC-8074-25F47400DB33','0D6C9C86-0754-490F-990C-A1E5C21C1FFC',1,0,0,1,NULL,NULL,'Group E',0)
,('FA000000-0000-0000-0000-000000000031','CA000000-0000-0000-0000-000000000001','2026-07-04 12:00:00','54AE3FEB-05C2-464F-8BDD-70AB56AB2A77','593CC788-2307-4160-B5BE-C2584DD82D9F',1,0,0,1,NULL,NULL,'Group F',0)
,('FA000000-0000-0000-0000-000000000032','CA000000-0000-0000-0000-000000000001','2026-07-07 12:00:00','54AE3FEB-05C2-464F-8BDD-70AB56AB2A77','7C8677FC-E653-46D4-A4CE-5134E143A1F0',1,0,0,1,NULL,NULL,'Group F',0)
,('FA000000-0000-0000-0000-000000000033','CA000000-0000-0000-0000-000000000001','2026-07-10 12:00:00','54AE3FEB-05C2-464F-8BDD-70AB56AB2A77','352447EE-EFB8-4AFD-AB78-986230D9F593',0,NULL,NULL,1,NULL,NULL,'Group F',0)
,('FA000000-0000-0000-0000-000000000034','CA000000-0000-0000-0000-000000000001','2026-07-10 12:00:00','593CC788-2307-4160-B5BE-C2584DD82D9F','7C8677FC-E653-46D4-A4CE-5134E143A1F0',0,NULL,NULL,1,NULL,NULL,'Group F',0)
,('FA000000-0000-0000-0000-000000000035','CA000000-0000-0000-0000-000000000001','2026-07-07 12:00:00','593CC788-2307-4160-B5BE-C2584DD82D9F','352447EE-EFB8-4AFD-AB78-986230D9F593',1,0,0,1,NULL,NULL,'Group F',0)
,('FA000000-0000-0000-0000-000000000036','CA000000-0000-0000-0000-000000000001','2026-07-04 12:00:00','7C8677FC-E653-46D4-A4CE-5134E143A1F0','352447EE-EFB8-4AFD-AB78-986230D9F593',1,0,0,1,NULL,NULL,'Group F',0)
,('FA000000-0000-0000-0000-000000000037','CA000000-0000-0000-0000-000000000001','2026-07-04 17:00:00','0D5F48CB-4DFB-45D5-9702-1576A7724F23','84E90FCC-4367-4429-A966-02578AF34002',1,0,0,1,NULL,NULL,'Group G',0)
,('FA000000-0000-0000-0000-000000000038','CA000000-0000-0000-0000-000000000001','2026-07-07 17:00:00','0D5F48CB-4DFB-45D5-9702-1576A7724F23','B5102EE3-73C2-453D-A228-041BACB690D1',1,0,0,1,NULL,NULL,'Group G',0)
,('FA000000-0000-0000-0000-000000000039','CA000000-0000-0000-0000-000000000001','2026-07-10 17:00:00','0D5F48CB-4DFB-45D5-9702-1576A7724F23','D5C1438E-2733-4FA8-99B9-099F1D4AF53C',0,NULL,NULL,1,NULL,NULL,'Group G',0)
,('FA000000-0000-0000-0000-000000000040','CA000000-0000-0000-0000-000000000001','2026-07-10 17:00:00','84E90FCC-4367-4429-A966-02578AF34002','B5102EE3-73C2-453D-A228-041BACB690D1',0,NULL,NULL,1,NULL,NULL,'Group G',0)
,('FA000000-0000-0000-0000-000000000041','CA000000-0000-0000-0000-000000000001','2026-07-07 17:00:00','84E90FCC-4367-4429-A966-02578AF34002','D5C1438E-2733-4FA8-99B9-099F1D4AF53C',1,0,0,1,NULL,NULL,'Group G',0)
,('FA000000-0000-0000-0000-000000000042','CA000000-0000-0000-0000-000000000001','2026-07-04 17:00:00','B5102EE3-73C2-453D-A228-041BACB690D1','D5C1438E-2733-4FA8-99B9-099F1D4AF53C',1,0,0,1,NULL,NULL,'Group G',0)
,('FA000000-0000-0000-0000-000000000043','CA000000-0000-0000-0000-000000000001','2026-07-04 17:00:00','E777CECE-FE34-44E4-B0EE-BF70CA3892F4','47B08B51-B7F3-4D5D-9136-013EE7473C09',1,0,0,1,NULL,NULL,'Group H',0)
,('FA000000-0000-0000-0000-000000000044','CA000000-0000-0000-0000-000000000001','2026-07-07 17:00:00','E777CECE-FE34-44E4-B0EE-BF70CA3892F4','A0981D60-F0EE-4DAC-88C6-8E5AA916247B',1,0,0,1,NULL,NULL,'Group H',0)
,('FA000000-0000-0000-0000-000000000045','CA000000-0000-0000-0000-000000000001','2026-07-10 17:00:00','E777CECE-FE34-44E4-B0EE-BF70CA3892F4','A1124BBF-23A4-427C-AE3E-19B81DF5EAB5',0,NULL,NULL,1,NULL,NULL,'Group H',0)
,('FA000000-0000-0000-0000-000000000046','CA000000-0000-0000-0000-000000000001','2026-07-10 17:00:00','47B08B51-B7F3-4D5D-9136-013EE7473C09','A0981D60-F0EE-4DAC-88C6-8E5AA916247B',0,NULL,NULL,1,NULL,NULL,'Group H',0)
,('FA000000-0000-0000-0000-000000000047','CA000000-0000-0000-0000-000000000001','2026-07-07 17:00:00','47B08B51-B7F3-4D5D-9136-013EE7473C09','A1124BBF-23A4-427C-AE3E-19B81DF5EAB5',1,0,0,1,NULL,NULL,'Group H',0)
,('FA000000-0000-0000-0000-000000000048','CA000000-0000-0000-0000-000000000001','2026-07-04 17:00:00','A0981D60-F0EE-4DAC-88C6-8E5AA916247B','A1124BBF-23A4-427C-AE3E-19B81DF5EAB5',1,0,0,1,NULL,NULL,'Group H',0)
,('FA000000-0000-0000-0000-000000000049','CA000000-0000-0000-0000-000000000001','2026-07-12 15:00:00',NULL,NULL,0,NULL,NULL,1,'Winner Group A','Runner-up Group B','Round of 16 1',1)
,('FA000000-0000-0000-0000-000000000050','CA000000-0000-0000-0000-000000000001','2026-07-12 19:00:00',NULL,NULL,0,NULL,NULL,1,'Winner Group C','Runner-up Group D','Round of 16 2',1)
,('FA000000-0000-0000-0000-000000000051','CA000000-0000-0000-0000-000000000001','2026-07-12 15:00:00',NULL,NULL,0,NULL,NULL,1,'Winner Group E','Runner-up Group F','Round of 16 3',1)
,('FA000000-0000-0000-0000-000000000052','CA000000-0000-0000-0000-000000000001','2026-07-12 19:00:00',NULL,NULL,0,NULL,NULL,1,'Winner Group G','Runner-up Group H','Round of 16 4',1)
,('FA000000-0000-0000-0000-000000000053','CA000000-0000-0000-0000-000000000001','2026-07-13 15:00:00',NULL,NULL,0,NULL,NULL,1,'Winner Group B','Runner-up Group A','Round of 16 5',1)
,('FA000000-0000-0000-0000-000000000054','CA000000-0000-0000-0000-000000000001','2026-07-13 19:00:00',NULL,NULL,0,NULL,NULL,1,'Winner Group D','Runner-up Group C','Round of 16 6',1)
,('FA000000-0000-0000-0000-000000000055','CA000000-0000-0000-0000-000000000001','2026-07-13 15:00:00',NULL,NULL,0,NULL,NULL,1,'Winner Group F','Runner-up Group E','Round of 16 7',1)
,('FA000000-0000-0000-0000-000000000056','CA000000-0000-0000-0000-000000000001','2026-07-13 19:00:00',NULL,NULL,0,NULL,NULL,1,'Winner Group H','Runner-up Group G','Round of 16 8',1)
,('FA000000-0000-0000-0000-000000000057','CA000000-0000-0000-0000-000000000001','2026-07-16 15:00:00',NULL,NULL,0,NULL,NULL,1,'Winner R16 1','Winner R16 2','Quarter Final 1',1)
,('FA000000-0000-0000-0000-000000000058','CA000000-0000-0000-0000-000000000001','2026-07-16 19:00:00',NULL,NULL,0,NULL,NULL,1,'Winner R16 3','Winner R16 4','Quarter Final 2',1)
,('FA000000-0000-0000-0000-000000000059','CA000000-0000-0000-0000-000000000001','2026-07-17 15:00:00',NULL,NULL,0,NULL,NULL,1,'Winner R16 5','Winner R16 6','Quarter Final 3',1)
,('FA000000-0000-0000-0000-000000000060','CA000000-0000-0000-0000-000000000001','2026-07-17 19:00:00',NULL,NULL,0,NULL,NULL,1,'Winner R16 7','Winner R16 8','Quarter Final 4',1)
,('FA000000-0000-0000-0000-000000000061','CA000000-0000-0000-0000-000000000001','2026-07-20 15:00:00',NULL,NULL,0,NULL,NULL,1,'Winner QF1','Winner QF2','Semi Final 1',1)
,('FA000000-0000-0000-0000-000000000062','CA000000-0000-0000-0000-000000000001','2026-07-20 19:00:00',NULL,NULL,0,NULL,NULL,1,'Winner QF3','Winner QF4','Semi Final 2',1)
,('FA000000-0000-0000-0000-000000000063','CA000000-0000-0000-0000-000000000001','2026-07-23 15:00:00',NULL,NULL,0,NULL,NULL,1,'Loser SF1','Loser SF2','3rd Place Play-off',1)
,('FA000000-0000-0000-0000-000000000064','CA000000-0000-0000-0000-000000000001','2026-07-24 15:00:00',NULL,NULL,0,NULL,NULL,1,'Winner SF1','Winner SF2','Final',1)
    ) AS [Raw] ([MatchID],[CompetitionID],[MatchDateTime],[LegacyHomeTeamID],[LegacyAwayTeamID],[MatchPlayed],
        [HomeTeamGoals],[AwayTeamGoals],[NeutralGround],[HomeTeamTBC],[AwayTeamTBC],[Description],[Knockout])
    LEFT JOIN #SampleCupTeamMap AS [HomeMap] ON [HomeMap].[LegacyTeamID] = [Raw].[LegacyHomeTeamID]
    LEFT JOIN #SampleCupTeamMap AS [AwayMap] ON [AwayMap].[LegacyTeamID] = [Raw].[LegacyAwayTeamID]
    LEFT JOIN [dbo].[Team] AS [HomeTeam] ON [HomeTeam].[TeamName] = [HomeMap].[TeamName]
    LEFT JOIN [dbo].[Team] AS [AwayTeam] ON [AwayTeam].[TeamName] = [AwayMap].[TeamName]
) AS [Source] ([MatchID],[CompetitionID],[MatchDateTime],[HomeTeamID],[AwayTeamID],[MatchPlayed],
    [HomeTeamGoals],[AwayTeamGoals],[NeutralGround],[HomeTeamTBC],[AwayTeamTBC],[Description],[Knockout])
ON ([Target].[MatchID] = [Source].[MatchID])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([MatchID],[CompetitionID],[MatchDateTime],[HomeTeamID],[AwayTeamID],[MatchPlayed],
        [HomeTeamGoals],[AwayTeamGoals],[NeutralGround],[HomeTeamTBC],[AwayTeamTBC],[Description],[Knockout])
    VALUES ([Source].[MatchID],[Source].[CompetitionID],[Source].[MatchDateTime],[Source].[HomeTeamID],
        [Source].[AwayTeamID],[Source].[MatchPlayed],[Source].[HomeTeamGoals],[Source].[AwayTeamGoals],
        [Source].[NeutralGround],[Source].[HomeTeamTBC],[Source].[AwayTeamTBC],[Source].[Description],
        [Source].[Knockout])
WHEN MATCHED THEN
    UPDATE SET
        [Target].[MatchDateTime] = [Source].[MatchDateTime],
        [Target].[HomeTeamID] = [Source].[HomeTeamID],
        [Target].[AwayTeamID] = [Source].[AwayTeamID],
        [Target].[MatchPlayed] = [Source].[MatchPlayed],
        [Target].[HomeTeamGoals] = [Source].[HomeTeamGoals],
        [Target].[AwayTeamGoals] = [Source].[AwayTeamGoals],
        [Target].[NeutralGround] = [Source].[NeutralGround],
        [Target].[HomeTeamTBC] = [Source].[HomeTeamTBC],
        [Target].[AwayTeamTBC] = [Source].[AwayTeamTBC],
        [Target].[Description] = [Source].[Description],
        [Target].[Knockout] = [Source].[Knockout];
GO

-- Every sample match carries an external id, without which the live-score poller ignores it (it has
-- no way to ask the provider about a fixture the provider can't identify). Taken from the last three
-- digits of the hand-authored MatchIDs above - 'FA000000-...-000000000057' becomes 57 - so the ids
-- are stable across re-seeds, unique within the competition, and legible against the fixture list.
UPDATE [dbo].[Match]
SET [ExternalMatchID] = CAST(RIGHT(CAST([MatchID] AS CHAR(36)), 3) AS INT)
WHERE [CompetitionID] = 'CA000000-0000-0000-0000-000000000001';
GO

-- Live scores are derived from the fixtures above and are reset with them. Re-seeding puts every
-- sample match back to unplayed, so a score left over from a previous run would show a match in
-- progress that has just been rewound to "not started".
DELETE FROM [dbo].[MatchLiveScore]
WHERE [MatchID] IN (
    SELECT [MatchID] FROM [dbo].[Match] WHERE [CompetitionID] = 'CA000000-0000-0000-0000-000000000001'
);
GO
