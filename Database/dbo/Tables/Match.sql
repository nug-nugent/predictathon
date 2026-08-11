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
    [ExternalMatchID] INT            NULL,
    CONSTRAINT [PK_Match] PRIMARY KEY CLUSTERED ([MatchID] ASC),
    CONSTRAINT [FK_Match_AwayTeam] FOREIGN KEY ([AwayTeamID]) REFERENCES [dbo].[Team] ([TeamID]),
    CONSTRAINT [FK_Match_Competition] FOREIGN KEY ([CompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_Match_HomeTeam] FOREIGN KEY ([HomeTeamID]) REFERENCES [dbo].[Team] ([TeamID])
);


GO
-- Links a Match to its external fixture-provider ID (e.g. football-data.org), so the fixture sync
-- job can detect reschedules by ExternalMatchID rather than fuzzy-matching on teams/date. Filtered
-- because most matches (older seasons, cup fixtures) have no external source and NULL shouldn't
-- collide under a unique constraint.
CREATE UNIQUE INDEX [IX_Match_ExternalMatchID] ON [dbo].[Match] ([ExternalMatchID] ASC) WHERE ([ExternalMatchID] IS NOT NULL);
GO

CREATE NONCLUSTERED INDEX [IX_Match_CompetitionID] ON [dbo].[Match]
(
	[CompetitionID] ASC
);
GO

CREATE NONCLUSTERED INDEX [IX_Match_HomeTeamID] ON [dbo].[Match]
(
	[HomeTeamID] ASC
);
GO

CREATE NONCLUSTERED INDEX [IX_Match_AwayTeamID] ON [dbo].[Match]
(
	[AwayTeamID] ASC
);
GO

