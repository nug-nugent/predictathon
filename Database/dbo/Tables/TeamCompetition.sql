CREATE TABLE [dbo].[TeamCompetition] (
    [TeamCompetitionID] UNIQUEIDENTIFIER NOT NULL,
    [TeamID]            UNIQUEIDENTIFIER NOT NULL,
    [CompetitionID]     UNIQUEIDENTIFIER NOT NULL,
    -- The group this team is drawn into for this competition (e.g. 'Group A'), for a tournament
    -- with a group stage. NULL for a competition that has no groups at all - a league season, say -
    -- which is why this lives here rather than on Team: a team's group is per-competition.
    [GroupName]         VARCHAR (20)     NULL,
    CONSTRAINT [PK_TeamCompetition] PRIMARY KEY CLUSTERED ([TeamCompetitionID] ASC),
    CONSTRAINT [FK_TeamCompetition_Competition] FOREIGN KEY ([CompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_TeamCompetition_Team] FOREIGN KEY ([TeamID]) REFERENCES [dbo].[Team] ([TeamID])
);
GO

CREATE NONCLUSTERED INDEX [IX_TeamCompetition_TeamID] ON [dbo].[TeamCompetition]
(
	[TeamID] ASC
);
GO

CREATE NONCLUSTERED INDEX [IX_TeamCompetition_CompetitionID] ON [dbo].[TeamCompetition]
(
	[CompetitionID] ASC
);
GO