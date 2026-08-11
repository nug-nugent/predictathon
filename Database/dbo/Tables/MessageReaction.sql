CREATE TABLE [dbo].[MessageReaction] (
    [MessageReactionID] UNIQUEIDENTIFIER NOT NULL,
    [MessageID] UNIQUEIDENTIFIER NOT NULL,
    [UserID] UNIQUEIDENTIFIER NOT NULL,
    [ReactionName] NVARCHAR(200) NOT NULL,
    [ImageUrl] NVARCHAR(500) NOT NULL, 
    [CreationDate] DATETIME NOT NULL , 
    CONSTRAINT [PK_MessageReaction] PRIMARY KEY NONCLUSTERED ([MessageReactionID] ASC),
    CONSTRAINT [FK_MessageReaction_Message] FOREIGN KEY ([MessageID]) REFERENCES [dbo].[Message] ([MessageID]),
    CONSTRAINT [FK_MessageReaction_User] FOREIGN KEY ([UserID]) REFERENCES [Identity].[Users] ([Id])
);

GO
CREATE INDEX [IX_MessageReaction_MessageID] ON [dbo].[MessageReaction] ([MessageID] ASC, [CreationDate] ASC);
GO
CREATE NONCLUSTERED INDEX [IX_MessageReaction_UserID] ON [dbo].[MessageReaction] ([UserID] ASC);

