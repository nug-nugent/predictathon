/*
Master script for the Docker dev stack's sample reference data. Run via:

  sqlcmd -S <server> -d Predictathon -U sa -P <password> -i 00_RunAll.sql

All sub-scripts are idempotent MERGEs (safe to re-run against an already-seeded database). Order
matters for FK dependencies: Competition before TeamCompetition/Match/UserCompetition, Users before
UserRoles/UserCompetition. Teams themselves are no longer seeded here - they're real reference data,
seeded by Database/Post-Deployment/Script.PostDeployment.sql as part of db-migrate's dacpac publish,
which always completes before this script runs.
*/

-- Identity.Users has a filtered unique index (UserNameIndex), which requires QUOTED_IDENTIFIER ON
-- for any MERGE/INSERT/UPDATE/DELETE against that table - set once here for the whole session.
SET QUOTED_IDENTIFIER ON;
GO

:r 02_Competition.sql
:r 03_TeamCompetition.sql
:r 04_Match.sql
:r 05_Users.sql
:r 06_UserRoles.sql
:r 07_UserCompetition.sql

PRINT 'Sample data seeded.';
GO
