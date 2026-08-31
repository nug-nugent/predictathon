/*
Ten more competitors for "Sample Cup", so the league table has a field rather than a pair.

Without them the sample competition has exactly two players (DemoAdmin and DemoPredictor), which
makes every table built on standings - the League page, the Live page's table, Statistics, the Home
page's mini table - a two-row stub, and makes position movement meaningless.

These accounts exist to be *listed*, not logged into: PasswordHash is NULL, so Identity has nothing
to check a password against and sign-in is impossible. Names are obviously fictional. Ids are fixed
rather than generated so 09_Predictions.sql's predictions stay attached to the same people across
re-seeds.
*/

SET NOCOUNT ON

DECLARE @Competitors TABLE (
    UserID UNIQUEIDENTIFIER NOT NULL,
    UserCompetitionID UNIQUEIDENTIFIER NOT NULL,
    UserName NVARCHAR(256) NOT NULL,
    Email NVARCHAR(256) NOT NULL
);

INSERT INTO @Competitors (UserID, UserCompetitionID, UserName, Email)
VALUES
 ('DA000000-0000-0000-0000-000000000001','AD000000-0000-0000-0000-000000000001','Ade Fletcher','ade.fletcher@example.com')
,('DA000000-0000-0000-0000-000000000002','AD000000-0000-0000-0000-000000000002','Bryn Callaghan','bryn.callaghan@example.com')
,('DA000000-0000-0000-0000-000000000003','AD000000-0000-0000-0000-000000000003','Cerys Openshaw','cerys.openshaw@example.com')
,('DA000000-0000-0000-0000-000000000004','AD000000-0000-0000-0000-000000000004','Dermot Whitely','dermot.whitely@example.com')
,('DA000000-0000-0000-0000-000000000005','AD000000-0000-0000-0000-000000000005','Esme Rawlinson','esme.rawlinson@example.com')
,('DA000000-0000-0000-0000-000000000006','AD000000-0000-0000-0000-000000000006','Fitz Barrowman','fitz.barrowman@example.com')
,('DA000000-0000-0000-0000-000000000007','AD000000-0000-0000-0000-000000000007','Greta Pemberton','greta.pemberton@example.com')
,('DA000000-0000-0000-0000-000000000008','AD000000-0000-0000-0000-000000000008','Hal Winterbourne','hal.winterbourne@example.com')
,('DA000000-0000-0000-0000-000000000009','AD000000-0000-0000-0000-000000000009','Ida Thornbury','ida.thornbury@example.com')
,('DA000000-0000-0000-0000-000000000010','AD000000-0000-0000-0000-000000000010','Jonty Marchetti','jonty.marchetti@example.com');

MERGE INTO [Identity].[Users] AS [Target]
USING @Competitors AS [Source]
ON ([Target].[Id] = [Source].[UserID])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],
        [PasswordHash],[SecurityStamp],[ConcurrencyStamp],[LockoutEnabled])
    VALUES ([Source].[UserID],[Source].[UserName],UPPER([Source].[UserName]),[Source].[Email],
        UPPER([Source].[Email]),1,
        NULL,CAST(NEWID() AS NVARCHAR(MAX)),CAST(NEWID() AS NVARCHAR(MAX)),1);

-- Registered into Sample Cup on the same free-entry terms 07_UserCompetition.sql uses for the demo
-- accounts. IsDefaultCompetition is 1 because it's the only competition any of them are in.
MERGE INTO [dbo].[UserCompetition] AS [Target]
USING @Competitors AS [Source]
ON ([Target].[UserCompetitionID] = [Source].[UserCompetitionID])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([UserCompetitionID],[UserID],[CompetitionID],[AmountPaid],[PaymentProvider],[PaymentCreditID],[IsDefaultCompetition])
    VALUES ([Source].[UserCompetitionID],[Source].[UserID],'CA000000-0000-0000-0000-000000000001',0.00,NULL,NULL,1);
GO
