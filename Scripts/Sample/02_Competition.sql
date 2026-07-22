/*
"Sample Cup" - a hand-crafted competition, not sourced from real data. Deliberately free
(EntranceFee = 0) and PayPal-disabled so registration always works with zero external
dependencies, regardless of the real World Cup competition's live registration/payment state.
*/

SET NOCOUNT ON

MERGE INTO [dbo].[Competition] AS [Target]
USING (VALUES (
    'CA000000-0000-0000-0000-000000000001',
    'Sample Cup',
    0,                                          -- PrependNameWithThe
    '2026-07-03', '2026-08-01',                 -- StartDate, EndDate
    0,                                          -- DuplicateFixturesAllowed
    1,                                          -- OpenForRegistration
    1,                                          -- RegistrationAvailableOnLoginPage
    1,                                          -- ShowInHallOfFame
    0.00,                                       -- EntranceFee
    0,                                          -- PayPalPaymentAvailable
    'A sample tournament seeded for local Docker development. 32 teams, 8 groups - the group stage and Round of 16 are already played, the Quarter-finals onward are still to be predicted.',
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
        [Source].[Information], [Source].[ImageFilename], [Source].[DefaultToNeutralGround]);
GO
