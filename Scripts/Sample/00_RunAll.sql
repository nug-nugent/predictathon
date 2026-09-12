/*
Master script for the Docker dev stack's sample reference data. Run via:

  sqlcmd -S <server> -d Predictathon -U sa -P <password> -i 00_RunAll.sql

All sub-scripts are idempotent (safe to re-run against an already-seeded database). Order
matters for FK dependencies: Competition before TeamCompetition/Match/UserCompetition, Users before
UserRoles/UserCompetition, and both Match and UserCompetition before Predictions.

Mostly MERGEs, but not purely: 09 clears predictions against fixtures that have not kicked off and 12
deletes leftover test accounts, because a seed that only ever tops up cannot put back what the e2e
suite consumes. Both are scoped to data the suite creates - see the headers of those two files.

Teams themselves are no longer seeded here - they're real reference data, seeded by
Database/Post-Deployment/Script.PostDeployment.sql as part of db-migrate's dacpac publish, which
always completes before this script runs.
*/

-- Identity.Users has a filtered unique index (UserNameIndex), which requires QUOTED_IDENTIFIER ON
-- for any MERGE/INSERT/UPDATE/DELETE against that table - set once here for the whole session.
SET QUOTED_IDENTIFIER ON;
GO

:r 01_SampleCupSetup.sql
:r 02_Competition.sql
:r 03_TeamCompetition.sql
:r 04_Match.sql
:r 05_Users.sql
:r 06_UserRoles.sql
:r 07_UserCompetition.sql
:r 08_SampleCompetitors.sql
:r 09_Predictions.sql
:r 10_HallOfFame.sql
:r 11_AdminCup.sql
-- Out of numeric order deliberately: 12 is a cleanup pass and belongs last, after everything that
-- puts data in. It is also the one script here that can fail on data it doesn't own (an account it
-- is deleting having picked up a row some other table holds), and a failure there shouldn't take
-- the seeding of a competition down with it.
:r 13_WeekAheadCup.sql
:r 12_PurgeTestAccounts.sql

PRINT 'Sample data seeded.';
GO
