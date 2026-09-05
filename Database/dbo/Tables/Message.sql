CREATE TABLE [dbo].[Message] (
    [MessageID]                  UNIQUEIDENTIFIER NOT NULL,
    [MessageThreadID]            UNIQUEIDENTIFIER NOT NULL,
    [PostedByUserID]             UNIQUEIDENTIFIER NOT NULL,
    [MessageDateTime]            DATETIME         CONSTRAINT [DF_Message_MessageDateTime] DEFAULT (getdate()) NOT NULL,
    [MessageContent]             NVARCHAR (MAX)   NULL,
    [YouTubeVideoID]             CHAR (11)        NULL,
    [HasLinkedImage]             BIT              CONSTRAINT [DF_Message_HasLinkedImage] DEFAULT ((0)) NOT NULL,
    [UserTotalMessageboardPosts] INT              CONSTRAINT [DF_Message_UserTotalMessageboardPosts] DEFAULT ((0)) NOT NULL,
    -- The message this one is a reply to, or NULL for an ordinary post. Self-referencing, but the
    -- board still renders as one flat chronological list: a reply shows a quoted stub of its
    -- parent rather than nesting under it, so there is no depth to bound and a reply to a reply
    -- needs no special handling. The service constrains the parent to the same thread.
    [ReplyToMessageID]           UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_Message] PRIMARY KEY CLUSTERED ([MessageID] ASC),
    CONSTRAINT [FK_Message_MessageThread] FOREIGN KEY ([MessageThreadID]) REFERENCES [dbo].[MessageThread] ([MessageThreadID]),
    CONSTRAINT [FK_Message_User] FOREIGN KEY ([PostedByUserID]) REFERENCES [Identity].[Users] ([Id]),
    -- No cascade: nothing deletes messages today, and if that ever changes, silently deleting
    -- every reply to a removed post is not what anyone would want.
    CONSTRAINT [FK_Message_Message_ReplyToMessageID] FOREIGN KEY ([ReplyToMessageID]) REFERENCES [dbo].[Message] ([MessageID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Message_MessageThreadID]
    ON [dbo].[Message]([MessageThreadID] ASC);
GO
CREATE NONCLUSTERED INDEX [IX_Message_PostedByUserID]
    ON [dbo].[Message]([PostedByUserID] ASC);
GO
-- Filtered: the overwhelming majority of messages aren't replies, and every query that cares about
-- this column is looking for the ones that are (resolving a page's parents, and any later
-- "replies to you" feed).
CREATE NONCLUSTERED INDEX [IX_Message_ReplyToMessageID]
    ON [dbo].[Message]([ReplyToMessageID] ASC) WHERE [ReplyToMessageID] IS NOT NULL;

