CREATE TABLE [dbo].[Import_Match] (
    [MatchDate]   DATETIME       NOT NULL,
    [MatchTime]   TIME (7)       NOT NULL,
    [HomeTeam]    NVARCHAR (255) COLLATE Latin1_General_CI_AI NULL,
    [AwayTeam]    NVARCHAR (255) COLLATE Latin1_General_CI_AI NULL,
    [Description] NVARCHAR (255) COLLATE Latin1_General_CI_AI NULL
);

