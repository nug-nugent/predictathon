CREATE TABLE [dbo].[HallOfFame] (
    [HallOfFameID]      UNIQUEIDENTIFIER NOT NULL,
    [CompetitionID]     UNIQUEIDENTIFIER NULL,
    [CompetitionName]   VARCHAR (50)     COLLATE Latin1_General_CI_AI NULL,
    [Winner]            VARCHAR (50)     COLLATE Latin1_General_CI_AI NULL,
    [WinnerUserID]      UNIQUEIDENTIFIER NULL,
    [SecondPlace]       VARCHAR (50)     COLLATE Latin1_General_CI_AI NULL,
    [SecondPlaceUserID] UNIQUEIDENTIFIER NULL,
    [ThirdPlace]        VARCHAR (50)     COLLATE Latin1_General_CI_AI NULL,
    [ThirdPlaceUserID]  UNIQUEIDENTIFIER NULL,
    [EndDate]           DATE             NOT NULL,
    [ImageFilename]     VARCHAR (40)     COLLATE Latin1_General_CI_AI NULL,
    CONSTRAINT [PK_HallOfFame] PRIMARY KEY CLUSTERED ([HallOfFameID] ASC),
    CONSTRAINT [FK_HallOfFame_Competition] FOREIGN KEY ([CompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_HallOfFame_User] FOREIGN KEY ([WinnerUserID]) REFERENCES [dbo].[User] ([UserID]),
    CONSTRAINT [FK_HallOfFame_User1] FOREIGN KEY ([SecondPlaceUserID]) REFERENCES [dbo].[User] ([UserID]),
    CONSTRAINT [FK_HallOfFame_User2] FOREIGN KEY ([ThirdPlaceUserID]) REFERENCES [dbo].[User] ([UserID])
);


GO
ALTER TABLE [dbo].[HallOfFame] NOCHECK CONSTRAINT [FK_HallOfFame_Competition];


GO
ALTER TABLE [dbo].[HallOfFame] NOCHECK CONSTRAINT [FK_HallOfFame_User];


GO
ALTER TABLE [dbo].[HallOfFame] NOCHECK CONSTRAINT [FK_HallOfFame_User1];


GO
ALTER TABLE [dbo].[HallOfFame] NOCHECK CONSTRAINT [FK_HallOfFame_User2];

