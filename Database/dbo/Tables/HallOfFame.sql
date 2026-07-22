CREATE TABLE [dbo].[HallOfFame] (
    [HallOfFameID]      UNIQUEIDENTIFIER NOT NULL,
    [CompetitionID]     UNIQUEIDENTIFIER NULL,
    [CompetitionName]   VARCHAR (50)     NULL,
    [Winner]            VARCHAR (50)     NULL,
    [WinnerUserID]      UNIQUEIDENTIFIER NULL,
    [SecondPlace]       VARCHAR (50)     NULL,
    [SecondPlaceUserID] UNIQUEIDENTIFIER NULL,
    [ThirdPlace]        VARCHAR (50)     NULL,
    [ThirdPlaceUserID]  UNIQUEIDENTIFIER NULL,
    [EndDate]           DATE             NOT NULL,
    [ImageFilename]     VARCHAR (40)     NULL,
    CONSTRAINT [PK_HallOfFame] PRIMARY KEY CLUSTERED ([HallOfFameID] ASC),
    CONSTRAINT [FK_HallOfFame_Competition] FOREIGN KEY ([CompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_HallOfFame_User] FOREIGN KEY ([WinnerUserID]) REFERENCES [Identity].[Users] ([Id]),
    CONSTRAINT [FK_HallOfFame_User1] FOREIGN KEY ([SecondPlaceUserID]) REFERENCES [Identity].[Users] ([Id]),
    CONSTRAINT [FK_HallOfFame_User2] FOREIGN KEY ([ThirdPlaceUserID]) REFERENCES [Identity].[Users] ([Id])
);


GO
ALTER TABLE [dbo].[HallOfFame] NOCHECK CONSTRAINT [FK_HallOfFame_Competition];


GO
ALTER TABLE [dbo].[HallOfFame] NOCHECK CONSTRAINT [FK_HallOfFame_User];


GO
ALTER TABLE [dbo].[HallOfFame] NOCHECK CONSTRAINT [FK_HallOfFame_User1];


GO
ALTER TABLE [dbo].[HallOfFame] NOCHECK CONSTRAINT [FK_HallOfFame_User2];

