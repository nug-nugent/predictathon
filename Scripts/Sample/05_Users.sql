/*
DemoAdmin / DemoPredictor - David's existing sample accounts, not new synthetic ones. Rows (Id,
PasswordHash, SecurityStamp, ConcurrencyStamp, profile fields) are copied verbatim from David's real
dev DB (2026-07-18), so these accounts behave identically in Docker - same login, same password.
Real passwords: DemoAdmin / DemoAdmin!2026, DemoPredictor / DemoPass123! (also in README.md).

Deliberately NOT sourced via sp_generate_merge against a live table, unlike 01_Teams.sql - the real
Identity.Users table has ~50 other rows with genuine personal data (real emails, real password
hashes) that must never end up in this repo. These two rows were selected and copied by hand.

DemoQuickPredict and DemoBracket are synthetic, and exist so that the three e2e specs which enter
predictions have a player each - the names say which spec owns which. A prediction row is keyed on (UserID, MatchID), so specs sharing one
account contend for the same rows and each one's saved score turns up in another's assertions -
predictions.spec used to work around that by deliberately taking the LAST open fixture, leaving the
first to quick-predict.spec. A user apiece removes the contention rather than choreographing around
it. They share DemoPredictor's password (DemoPass123!) by sharing its PasswordHash verbatim, which
works because an ASP.NET Identity hash carries its own salt and is not bound to the account it sits
on. Their SecurityStamp/ConcurrencyStamp are their own.

Deliberately not "DemoPredictor2" and "DemoPredictor3". Playwright matches an accessible name by
substring unless a locator says exact, so a table row asking for "DemoPredictor" would answer with
all three and fail as ambiguous - which is exactly what league.spec did when they were numbered.
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
), (
    'DB000000-0000-0000-0000-000000000002',
    'DemoQuickPredict', 'DEMOQUICKPREDICT',
    'demo.quickpredict@example.com', 'DEMO.QUICKPREDICT@EXAMPLE.COM',
    0,
    'AQAAAAIAAYagAAAAEGDqw0HkR+FSDBhLl5X43m9iqPDEaSSUmbJS0Wbwlsh6eV87t0nAUhGUroT7kEP1EQ==',
    'B4XKQ7NRZP2WVDHY6MSJT8CFLU3AGEKQ',
    'c1f0a2d4-6b58-4e37-9a21-5d8f3c7b0e64',
    0, 1, 0,
    0, 0, 1
), (
    'DB000000-0000-0000-0000-000000000003',
    'DemoBracket', 'DEMOBRACKET',
    'demo.bracket@example.com', 'DEMO.BRACKET@EXAMPLE.COM',
    0,
    'AQAAAAIAAYagAAAAEGDqw0HkR+FSDBhLl5X43m9iqPDEaSSUmbJS0Wbwlsh6eV87t0nAUhGUroT7kEP1EQ==',
    'M9TZC5XWQK4RJNVB2HDPY7SGFA6LU3EO',
    '7a3e9b41-08cd-4f26-b5d9-2e6417ca0f83',
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
