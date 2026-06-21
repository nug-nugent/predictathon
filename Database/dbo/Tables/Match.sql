CREATE TABLE [dbo].[Match] (
    [MatchID]       UNIQUEIDENTIFIER NOT NULL,
    [CompetitionID] UNIQUEIDENTIFIER NOT NULL,
    [MatchDateTime] DATETIME         NOT NULL,
    [HomeTeamID]    UNIQUEIDENTIFIER NULL,
    [AwayTeamID]    UNIQUEIDENTIFIER NULL,
    [MatchPlayed]   BIT              CONSTRAINT [DF_Match_MatchPlayed] DEFAULT ((0)) NOT NULL,
    [HomeTeamGoals] INT              NULL,
    [AwayTeamGoals] INT              NULL,
    [NeutralGround] BIT              CONSTRAINT [DF_Match_NeutralGround] DEFAULT ((0)) NOT NULL,
    [HomeTeamTBC]   VARCHAR (50)     NULL,
    [AwayTeamTBC]   VARCHAR (50)     NULL,
    [Description]   VARCHAR (50)     NULL,
    [Knockout]      BIT              CONSTRAINT [DF_Match_Knockout] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Match] PRIMARY KEY CLUSTERED ([MatchID] ASC),
    CONSTRAINT [FK_Match_AwayTeam] FOREIGN KEY ([AwayTeamID]) REFERENCES [dbo].[Team] ([TeamID]),
    CONSTRAINT [FK_Match_Competition] FOREIGN KEY ([CompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_Match_HomeTeam] FOREIGN KEY ([HomeTeamID]) REFERENCES [dbo].[Team] ([TeamID])
);


GO
ALTER TABLE [dbo].[Match] NOCHECK CONSTRAINT [FK_Match_AwayTeam];


GO
ALTER TABLE [dbo].[Match] NOCHECK CONSTRAINT [FK_Match_Competition];


GO
ALTER TABLE [dbo].[Match] NOCHECK CONSTRAINT [FK_Match_HomeTeam];

