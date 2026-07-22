/*
Seeds the 3 admin roles with fixed, well-known GUIDs, so anything that needs a deterministic role
ID at DB-deploy time (e.g. Scripts/Sample's sample-data seeding for the Docker dev stack) doesn't
have to wait for or guess at IdentityExtensions.SeedRolesAsync's app-startup, NEWID()-per-environment
seeding.

MERGE keyed on NormalizedName, insert-only-when-missing, so this is safe everywhere:
- On a fresh DB (Docker, or any newly-provisioned environment), it creates the 3 roles with these
  fixed IDs immediately as part of schema deployment.
- On an already-seeded DB (e.g. prod, or a real dev DB where SeedRolesAsync already ran with random
  IDs), the NormalizedName match means this inserts nothing and touches nothing - existing
  Identity.UserRoles FK references are never at risk.

IdentityExtensions.SeedRolesAsync is left in place as a defensive fallback for any DB provisioned
outside this dacpac.

Idempotent: safe to re-run against a database this has already run against.
*/

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

MERGE [Identity].[Roles] AS target
USING (VALUES
    ('11111111-1111-1111-1111-111111111111', 'MatchAdministrator', 'MATCHADMINISTRATOR'),
    ('22222222-2222-2222-2222-222222222222', 'UserAdministrator', 'USERADMINISTRATOR'),
    ('33333333-3333-3333-3333-333333333333', 'CompetitionAdministrator', 'COMPETITIONADMINISTRATOR')
) AS source ([Id], [Name], [NormalizedName])
ON target.[NormalizedName] = source.[NormalizedName]
WHEN NOT MATCHED THEN
    INSERT ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (CONVERT(UNIQUEIDENTIFIER, source.[Id]), source.[Name], source.[NormalizedName], CONVERT(NVARCHAR(MAX), NEWID()));
