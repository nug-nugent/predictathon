CREATE TABLE [dbo].[HallOfFame] (
    [HallOfFameID]        UNIQUEIDENTIFIER NOT NULL,
    [CompetitionID]       UNIQUEIDENTIFIER NULL,
    [CompetitionName]     VARCHAR (50)     NULL,
    [Winner]              VARCHAR (50)     NULL,
    [WinnerUserID]        UNIQUEIDENTIFIER NULL,
    [SecondPlace]         VARCHAR (50)     NULL,
    [SecondPlaceUserID]   UNIQUEIDENTIFIER NULL,
    [ThirdPlace]          VARCHAR (50)     NULL,
    [ThirdPlaceUserID]    UNIQUEIDENTIFIER NULL,
    [EndDate]             DATE             NOT NULL,
    [ImageFilename]       VARCHAR (40)     NULL,
    [CompetitionSeriesID] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_HallOfFame] PRIMARY KEY CLUSTERED ([HallOfFameID] ASC),
    CONSTRAINT [FK_HallOfFame_Competition] FOREIGN KEY ([CompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_HallOfFame_User] FOREIGN KEY ([WinnerUserID]) REFERENCES [Identity].[Users] ([Id]),
    CONSTRAINT [FK_HallOfFame_User1] FOREIGN KEY ([SecondPlaceUserID]) REFERENCES [Identity].[Users] ([Id]),
    CONSTRAINT [FK_HallOfFame_User2] FOREIGN KEY ([ThirdPlaceUserID]) REFERENCES [Identity].[Users] ([Id]),
    CONSTRAINT [FK_HallOfFame_CompetitionSeries_CompetitionSeriesID] FOREIGN KEY ([CompetitionSeriesID]) REFERENCES [dbo].[CompetitionSeries] ([CompetitionSeriesID])
);
GO

CREATE NONCLUSTERED INDEX [IX_HallOfFame_CompetitionSeriesID] ON [dbo].[HallOfFame]
(
	[CompetitionSeriesID] ASC
);
GO

CREATE NONCLUSTERED INDEX [IX_HallOfFame_CompetitionID] ON [dbo].[HallOfFame]
(
	[CompetitionID] ASC
);
GO

CREATE NONCLUSTERED INDEX [IX_HallOfFame_WinnerUserID] ON [dbo].[HallOfFame]
(
	[WinnerUserID] ASC
);
GO

CREATE NONCLUSTERED INDEX [IX_HallOfFame_SecondPlaceUserID] ON [dbo].[HallOfFame]
(
	[SecondPlaceUserID] ASC
);
GO

CREATE NONCLUSTERED INDEX [IX_HallOfFame_ThirdPlaceUserID] ON [dbo].[HallOfFame]
(
	[ThirdPlaceUserID] ASC
);
GO

