CREATE TABLE [dbo].[Import_Match] (
    [MatchDate]   DATETIME       NOT NULL,
    [MatchTime]   TIME (7)       NOT NULL,
    [HomeTeam]    NVARCHAR (255) NULL,
    [AwayTeam]    NVARCHAR (255) NULL,
    [Description] NVARCHAR (255) NULL
);

