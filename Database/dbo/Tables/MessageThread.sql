CREATE TABLE [dbo].[MessageThread] (
    [MessageThreadID]  UNIQUEIDENTIFIER NOT NULL,
    [ThreadSubject]    VARCHAR (50)     NULL,
    [StartedByUserID]  UNIQUEIDENTIFIER NOT NULL,
    [StartedDateTime]  DATETIME         CONSTRAINT [DF_MessageThread_StartedDateTime] DEFAULT (getdate()) NOT NULL,
    [HiddenFromPublic] BIT              CONSTRAINT [DF_MessageThread_HiddenFromPublic] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_MessageThread] PRIMARY KEY CLUSTERED ([MessageThreadID] ASC),
    CONSTRAINT [FK_MessageThread_User] FOREIGN KEY ([StartedByUserID]) REFERENCES [Identity].[Users] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_MessageThread_StartedByUserID] ON [dbo].[MessageThread]
(
	[StartedByUserID] ASC
);
GO

