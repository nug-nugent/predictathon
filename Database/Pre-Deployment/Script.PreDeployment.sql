/*
Retires the legacy dbo.User table.

1. Drops the FK constraints that reference dbo.User, so the SSDT-managed schema changes later in
   this same deployment can drop the table itself.
2. Copies any dbo.User account that has never logged into the new Identity-based system (no
   matching Identity.Users row yet) across to Identity.Users, including granting Identity roles
   for the legacy MatchAdministrator/UserAdministrator/CompetitionAdministrator bit flags.
   Migrated accounts get PasswordHash = NULL - AuthService.Login detects that and directs the user
   through password reset instead of a normal login failure (see PasswordResetRequiredError).

The whole thing (not just steps 2-4) is guarded by dbo.User still existing. Originally only 2-4 had
that guard - step 1 dropped the FK constraints unconditionally on every deployment, on the theory
that it was harmless once dbo.User was gone (nothing left to protect by dropping them). That held
right up until a later deployment needed to change one of those same FKs' definitions (e.g.
enabling WITH CHECK) - SSDT's own generated script then tried to drop the same constraint this
script had just unconditionally dropped a moment earlier, and failed with "is not a constraint"
since it was already gone. Guarding step 1 too makes this a true no-op once dbo.User is retired,
which is what "idempotent" was always meant to cover.

Idempotent: safe to re-run against a database this has already run against, and safe to re-run
after dbo.User itself has been dropped by a later deployment.
*/

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

IF OBJECT_ID('dbo.User', 'U') IS NOT NULL
BEGIN
    -- 1. Drop FKs referencing dbo.User.
    IF OBJECT_ID('dbo.FK_HallOfFame_User', 'F') IS NOT NULL ALTER TABLE [dbo].[HallOfFame] DROP CONSTRAINT [FK_HallOfFame_User];
    IF OBJECT_ID('dbo.FK_HallOfFame_User1', 'F') IS NOT NULL ALTER TABLE [dbo].[HallOfFame] DROP CONSTRAINT [FK_HallOfFame_User1];
    IF OBJECT_ID('dbo.FK_HallOfFame_User2', 'F') IS NOT NULL ALTER TABLE [dbo].[HallOfFame] DROP CONSTRAINT [FK_HallOfFame_User2];
    IF OBJECT_ID('dbo.FK_MessageReaction_User', 'F') IS NOT NULL ALTER TABLE [dbo].[MessageReaction] DROP CONSTRAINT [FK_MessageReaction_User];
    IF OBJECT_ID('dbo.FK_Message_User', 'F') IS NOT NULL ALTER TABLE [dbo].[Message] DROP CONSTRAINT [FK_Message_User];
    IF OBJECT_ID('dbo.FK_MessageRating_User', 'F') IS NOT NULL ALTER TABLE [dbo].[MessageRating] DROP CONSTRAINT [FK_MessageRating_User];
    IF OBJECT_ID('dbo.FK_Transaction_User', 'F') IS NOT NULL ALTER TABLE [dbo].[Transaction] DROP CONSTRAINT [FK_Transaction_User];
    IF OBJECT_ID('dbo.FK_PaymentCredit_IssuedByUser', 'F') IS NOT NULL ALTER TABLE [dbo].[PaymentCredit] DROP CONSTRAINT [FK_PaymentCredit_IssuedByUser];
    IF OBJECT_ID('dbo.FK_PaymentCredit_UsedByUser', 'F') IS NOT NULL ALTER TABLE [dbo].[PaymentCredit] DROP CONSTRAINT [FK_PaymentCredit_UsedByUser];
    IF OBJECT_ID('dbo.FK_MessageThread_User', 'F') IS NOT NULL ALTER TABLE [dbo].[MessageThread] DROP CONSTRAINT [FK_MessageThread_User];
    IF OBJECT_ID('dbo.FK_UserCompetition_User', 'F') IS NOT NULL ALTER TABLE [dbo].[UserCompetition] DROP CONSTRAINT [FK_UserCompetition_User];
    IF OBJECT_ID('dbo.FK_MessageThreadRead_User', 'F') IS NOT NULL ALTER TABLE [dbo].[MessageThreadRead] DROP CONSTRAINT [FK_MessageThreadRead_User];
    IF OBJECT_ID('dbo.FK_Prediction_User', 'F') IS NOT NULL ALTER TABLE [dbo].[Prediction] DROP CONSTRAINT [FK_Prediction_User];

    -- 2. Seed the 3 admin roles if they don't already exist. Normally done by
    -- IdentityExtensions.SeedRolesAsync at app startup, but that isn't guaranteed to have run yet
    -- at DB-deploy time, and the role grants below need the roles to exist now.
    MERGE [Identity].[Roles] AS target
    USING (VALUES
        ('MatchAdministrator', 'MATCHADMINISTRATOR'),
        ('UserAdministrator', 'USERADMINISTRATOR'),
        ('CompetitionAdministrator', 'COMPETITIONADMINISTRATOR')
    ) AS source ([Name], [NormalizedName])
    ON target.[NormalizedName] = source.[NormalizedName]
    WHEN NOT MATCHED THEN
        INSERT ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
        VALUES (NEWID(), source.[Name], source.[NormalizedName], CONVERT(NVARCHAR(MAX), NEWID()));

    -- 3. Copy dbo.User rows with no matching Identity.Users row yet. Accounts that already exist
    -- in Identity.Users registered fresh under the new system and are already the source of truth
    -- there - never overwritten. Skips any row whose username would collide with an existing,
    -- unrelated Identity.Users account rather than fail the whole deployment on one bad row.
    INSERT INTO [Identity].[Users] (
        [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
        [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
        [Forenames], [Surname], [FavouriteTeam], [Location], [Caption], [ProfileText],
        [LastLoginDateTime], [ImageUploaded], [TotalMessageboardPosts], [EmailPredictionReminderDays],
        [CanViewHiddenMessageThreads], [CanViewMessageboard]
    )
    SELECT
        u.[UserID], u.[Username], UPPER(u.[Username]), u.[EmailAddress], UPPER(u.[EmailAddress]), 0,
        NULL, CONVERT(NVARCHAR(MAX), NEWID()), CONVERT(NVARCHAR(MAX), NEWID()),
        u.[Forenames], u.[Surname], u.[FavouriteTeam], u.[Location], u.[Caption], u.[ProfileText],
        u.[LastLoginDateTime], u.[ImageUploaded], u.[TotalMessageboardPosts], u.[EmailPredictionReminderDays],
        u.[CanViewHiddenMessageThreads], u.[CanViewMessageboard]
    FROM [dbo].[User] u
    WHERE NOT EXISTS (SELECT 1 FROM [Identity].[Users] iu WHERE iu.[Id] = u.[UserID])
      AND NOT EXISTS (SELECT 1 FROM [Identity].[Users] iu WHERE iu.[NormalizedUserName] = UPPER(u.[Username]));

    -- 4. Grant Identity roles matching the migrated accounts' legacy admin flags. Based on all of
    -- dbo.User (not just rows copied just now) so this stays correct on re-run; freshly-registered
    -- accounts never have these bits set (EnsureDboUserAsync always defaults them to false), so
    -- this only ever grants roles to genuinely-migrated legacy accounts in practice.
    INSERT INTO [Identity].[UserRoles] ([UserId], [RoleId])
    SELECT u.[UserID], r.[Id]
    FROM [dbo].[User] u
    INNER JOIN [Identity].[Users] iu ON iu.[Id] = u.[UserID]
    INNER JOIN [Identity].[Roles] r ON r.[NormalizedName] = 'MATCHADMINISTRATOR'
    WHERE u.[MatchAdministrator] = 1
      AND NOT EXISTS (SELECT 1 FROM [Identity].[UserRoles] ur WHERE ur.[UserId] = u.[UserID] AND ur.[RoleId] = r.[Id]);

    INSERT INTO [Identity].[UserRoles] ([UserId], [RoleId])
    SELECT u.[UserID], r.[Id]
    FROM [dbo].[User] u
    INNER JOIN [Identity].[Users] iu ON iu.[Id] = u.[UserID]
    INNER JOIN [Identity].[Roles] r ON r.[NormalizedName] = 'USERADMINISTRATOR'
    WHERE u.[UserAdministrator] = 1
      AND NOT EXISTS (SELECT 1 FROM [Identity].[UserRoles] ur WHERE ur.[UserId] = u.[UserID] AND ur.[RoleId] = r.[Id]);

    INSERT INTO [Identity].[UserRoles] ([UserId], [RoleId])
    SELECT u.[UserID], r.[Id]
    FROM [dbo].[User] u
    INNER JOIN [Identity].[Users] iu ON iu.[Id] = u.[UserID]
    INNER JOIN [Identity].[Roles] r ON r.[NormalizedName] = 'COMPETITIONADMINISTRATOR'
    WHERE u.[CompetitionAdministrator] = 1
      AND NOT EXISTS (SELECT 1 FROM [Identity].[UserRoles] ur WHERE ur.[UserId] = u.[UserID] AND ur.[RoleId] = r.[Id]);
END
