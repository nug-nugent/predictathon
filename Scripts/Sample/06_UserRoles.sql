/*
Grants DemoAdmin all 3 admin roles, using the fixed role GUIDs seeded by
Database/Post-Deployment/Script.PostDeployment.sql (guaranteed to exist by the time this runs,
since db-migrate's dacpac publish - which runs the post-deployment script - completes before
00_RunAll.sql does). DemoPredictor gets no roles - matches David's real dev DB.
*/

SET NOCOUNT ON

MERGE INTO [Identity].[UserRoles] AS [Target]
USING (VALUES
 ('CC44CE3F-CC89-44E9-98E1-08DEE1D3E748','11111111-1111-1111-1111-111111111111') -- MatchAdministrator
,('CC44CE3F-CC89-44E9-98E1-08DEE1D3E748','22222222-2222-2222-2222-222222222222') -- UserAdministrator
,('CC44CE3F-CC89-44E9-98E1-08DEE1D3E748','33333333-3333-3333-3333-333333333333') -- CompetitionAdministrator
) AS [Source] ([UserId],[RoleId])
ON ([Target].[UserId] = [Source].[UserId] AND [Target].[RoleId] = [Source].[RoleId])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([UserId],[RoleId])
    VALUES ([Source].[UserId],[Source].[RoleId]);
GO
