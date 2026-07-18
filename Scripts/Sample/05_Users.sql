/*
DemoAdmin / DemoPredictor - David's existing sample accounts, not new synthetic ones. Rows (Id,
PasswordHash, SecurityStamp, ConcurrencyStamp, profile fields) are copied verbatim from David's real
dev DB (2026-07-18), so these accounts behave identically in Docker - same login, same password.
Real passwords: DemoAdmin / DemoAdmin!2026, DemoPredictor / DemoPass123! (also in README.md).

Deliberately NOT sourced via sp_generate_merge against a live table, unlike 01_Teams.sql - the real
Identity.Users table has ~50 other rows with genuine personal data (real emails, real password
hashes) that must never end up in this repo. These two rows were selected and copied by hand.
*/

SET NOCOUNT ON

MERGE INTO [Identity].[Users] AS [Target]
USING (VALUES (
    'CC44CE3F-CC89-44E9-98E1-08DEE1D3E748',
    'DemoAdmin', 'DEMOADMIN',
    'demoadmin@example.com', 'DEMOADMIN@EXAMPLE.COM',
    0,
    'AQAAAAIAAYagAAAAECAImldSJ7LycZ6w6l4nQ8Eot3KJbdMGwDkIACkM0/3xY1lKO+DD5IvIemY4uQWkLQ==',
    'WTYHKSXVWG2WXNWU6XYXJOJ4OCZXUXHV',
    'fcb677d1-e3f8-4faf-8e4f-71aa61f7a140',
    0, 1, 0,
    3, 0, 1
), (
    'CA4D2ADD-8728-430C-4712-08DEE0003952',
    'DemoPredictor', 'DEMOPREDICTOR',
    'demo.predictor@example.com', 'DEMO.PREDICTOR@EXAMPLE.COM',
    0,
    'AQAAAAIAAYagAAAAEGDqw0HkR+FSDBhLl5X43m9iqPDEaSSUmbJS0Wbwlsh6eV87t0nAUhGUroT7kEP1EQ==',
    'XTDV5BU6SDO7QRUMTFRTNVTZJVAZGZMN',
    '0828dcd6-3299-4c41-9f93-7d11f465b5d8',
    0, 1, 0,
    0, 0, 1
)) AS [Source] (
    [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
    [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
    [PhoneNumberConfirmed], [LockoutEnabled], [AccessFailedCount],
    [TotalMessageboardPosts], [CanViewHiddenMessageThreads], [CanViewMessageboard]
)
ON ([Target].[Id] = [Source].[Id])
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
        [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
        [PhoneNumberConfirmed], [LockoutEnabled], [AccessFailedCount],
        [TotalMessageboardPosts], [CanViewHiddenMessageThreads], [CanViewMessageboard])
    VALUES ([Source].[Id], [Source].[UserName], [Source].[NormalizedUserName], [Source].[Email],
        [Source].[NormalizedEmail], [Source].[EmailConfirmed],
        [Source].[PasswordHash], [Source].[SecurityStamp], [Source].[ConcurrencyStamp],
        [Source].[PhoneNumberConfirmed], [Source].[LockoutEnabled], [Source].[AccessFailedCount],
        [Source].[TotalMessageboardPosts], [Source].[CanViewHiddenMessageThreads], [Source].[CanViewMessageboard]);
GO
