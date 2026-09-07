/*
Registers the four demo accounts into "Sample Cup", matching what
UserCompetitionService.CreateUserCompetitionAsync's free-registration path produces
(AmountPaid = 0, PaymentProvider = NULL, IsDefaultCompetition = 1 - it's each account's only
competition at this point in the seed, so it's unconditionally their default).

DemoAdmin doesn't stay that way: 11_AdminCup.sql registers them into a competition of their own and
demotes this row, so the e2e suite's admin specs enter results somewhere the player specs aren't
reading. This file keeps describing the plain registration and lets that one own the redirection.
*/

SET NOCOUNT ON

MERGE INTO [dbo].[UserCompetition] AS [Target]
USING (VALUES
 ('AC000000-0000-0000-0000-000000000001','CC44CE3F-CC89-44E9-98E1-08DEE1D3E748','CA000000-0000-0000-0000-000000000001',0.00,NULL,NULL,1)
,('AC000000-0000-0000-0000-000000000002','CA4D2ADD-8728-430C-4712-08DEE0003952','CA000000-0000-0000-0000-000000000001',0.00,NULL,NULL,1)
,('AC000000-0000-0000-0000-000000000003','DB000000-0000-0000-0000-000000000002','CA000000-0000-0000-0000-000000000001',0.00,NULL,NULL,1)
,('AC000000-0000-0000-0000-000000000004','DB000000-0000-0000-0000-000000000003','CA000000-0000-0000-0000-000000000001',0.00,NULL,NULL,1)
) AS [Source] ([UserCompetitionID],[UserID],[CompetitionID],[AmountPaid],[PaymentProvider],[PaymentCreditID],[IsDefaultCompetition])
ON ([Target].[UserCompetitionID] = [Source].[UserCompetitionID])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([UserCompetitionID],[UserID],[CompetitionID],[AmountPaid],[PaymentProvider],[PaymentCreditID],[IsDefaultCompetition])
    VALUES ([Source].[UserCompetitionID],[Source].[UserID],[Source].[CompetitionID],[Source].[AmountPaid],
        [Source].[PaymentProvider],[Source].[PaymentCreditID],[Source].[IsDefaultCompetition]);
GO
