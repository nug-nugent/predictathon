CREATE TABLE [dbo].[Announcement] (
    [AnnouncementID]    INT              IDENTITY (1, 1) NOT NULL,
    [Content]           NVARCHAR (2000)  NOT NULL,
    [ShowOnLoginPage]   BIT              CONSTRAINT [DF_Announcement_ShowOnLoginPage] DEFAULT ((0)) NOT NULL,
    [ShowOnHomepage]    BIT              CONSTRAINT [DF_Announcement_ShowOnHomepage] DEFAULT ((1)) NOT NULL,
    [Severity]          VARCHAR (20)     CONSTRAINT [DF_Announcement_Severity] DEFAULT ('Info') NOT NULL,
    [ExpiryDateTimeUtc] DATETIME         NULL,
    [CreatedAtUtc]      DATETIME         CONSTRAINT [DF_Announcement_CreatedAtUtc] DEFAULT (sysutcdatetime()) NOT NULL,
    [CreatedByUserID]   UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_Announcement] PRIMARY KEY CLUSTERED ([AnnouncementID] ASC),
    CONSTRAINT [FK_Announcement_CreatedByUser] FOREIGN KEY ([CreatedByUserID]) REFERENCES [Identity].[Users] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_Announcement_CreatedByUserID] ON [dbo].[Announcement]
(
	[CreatedByUserID] ASC
);
GO
