CREATE TABLE [dbo].[MessageThread] (
    [MessageThreadID]  UNIQUEIDENTIFIER NOT NULL,
    [ThreadSubject]    VARCHAR (50)     COLLATE Latin1_General_CI_AI NULL,
    [StartedByUserID]  UNIQUEIDENTIFIER NOT NULL,
    [StartedDateTime]  DATETIME         CONSTRAINT [DF_MessageThread_StartedDateTime] DEFAULT (getdate()) NOT NULL,
    [HiddenFromPublic] BIT              CONSTRAINT [DF_MessageThread_HiddenFromPublic] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_MessageThread] PRIMARY KEY CLUSTERED ([MessageThreadID] ASC),
    CONSTRAINT [FK_MessageThread_User] FOREIGN KEY ([StartedByUserID]) REFERENCES [dbo].[User] ([UserID])
);


GO
ALTER TABLE [dbo].[MessageThread] NOCHECK CONSTRAINT [FK_MessageThread_User];

