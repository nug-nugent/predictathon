/*
Seeds reference data that should exist identically in every environment - real, deployed via SSDT
rather than left to app-level or Docker-only sample seeding. Each included script is its own
idempotent MERGE, safe to re-run against a database it's already run against.
*/

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- Seeds the 3 admin roles with fixed, well-known GUIDs
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

-- Real Team data (forward slash, not backslash - the Docker db-migrate image builds this dacpac on
-- Linux, where a backslash is treated as part of the filename)
:r ReferenceData/01_Teams.sql
