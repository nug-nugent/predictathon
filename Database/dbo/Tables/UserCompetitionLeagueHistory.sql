CREATE TABLE [dbo].[UserCompetitionLeagueHistory] (
    [UserCompetitionLeagueHistoryID] UNIQUEIDENTIFIER CONSTRAINT [DF_Table_1_LeagueHistoryID] DEFAULT (newid()) NOT NULL,
    [UserCompetitionID]              UNIQUEIDENTIFIER NOT NULL,
    [Date]                           DATE             NOT NULL,
    [LeaguePosition]                 INT              NOT NULL,
    [Score]                          INT              NOT NULL,
    [AverageGoalDifference]          DECIMAL (9, 2)   NOT NULL,
    [TotalGoalDifference]            INT              NOT NULL,
    CONSTRAINT [PK_UserCompetitionLeagueHistory] PRIMARY KEY CLUSTERED ([UserCompetitionLeagueHistoryID] ASC)
);

