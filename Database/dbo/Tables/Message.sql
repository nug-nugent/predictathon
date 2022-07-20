CREATE TABLE [dbo].[Message] (
    [MessageID]                  UNIQUEIDENTIFIER NOT NULL,
    [MessageThreadID]            UNIQUEIDENTIFIER NOT NULL,
    [PostedByUserID]             UNIQUEIDENTIFIER NOT NULL,
    [MessageDateTime]            DATETIME         CONSTRAINT [DF_Message_MessageDateTime] DEFAULT (getdate()) NOT NULL,
    [MessageContent]             NVARCHAR (MAX)   NULL,
    [YouTubeVideoID]             CHAR (11)        COLLATE Latin1_General_CI_AI NULL,
    [HasLinkedImage]             BIT              CONSTRAINT [DF_Message_HasLinkedImage] DEFAULT ((0)) NOT NULL,
    [UserTotalMessageboardPosts] INT              CONSTRAINT [DF_Message_UserTotalMessageboardPosts] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Message] PRIMARY KEY CLUSTERED ([MessageID] ASC),
    CONSTRAINT [FK_Message_MessageThread] FOREIGN KEY ([MessageThreadID]) REFERENCES [dbo].[MessageThread] ([MessageThreadID]),
    CONSTRAINT [FK_Message_User] FOREIGN KEY ([PostedByUserID]) REFERENCES [dbo].[User] ([UserID])
);


GO
ALTER TABLE [dbo].[Message] NOCHECK CONSTRAINT [FK_Message_MessageThread];


GO
ALTER TABLE [dbo].[Message] NOCHECK CONSTRAINT [FK_Message_User];




GO
ALTER TABLE [dbo].[Message] NOCHECK CONSTRAINT [FK_Message_MessageThread];


GO
ALTER TABLE [dbo].[Message] NOCHECK CONSTRAINT [FK_Message_User];


GO
CREATE NONCLUSTERED INDEX [IX_Message_MessageThreadID]
    ON [dbo].[Message]([MessageThreadID] ASC);

