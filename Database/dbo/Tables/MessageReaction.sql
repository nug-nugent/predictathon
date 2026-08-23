CREATE TABLE [dbo].[MessageReaction] (
    [MessageReactionID] UNIQUEIDENTIFIER NOT NULL,
    [MessageID] UNIQUEIDENTIFIER NOT NULL,
    [UserID] UNIQUEIDENTIFIER NOT NULL,
    [ReactionName] NVARCHAR(200) NOT NULL,
    -- Namespaced reaction identity - 'u:{unified}' for a standard emoji, 'c:{id}' for one of
    -- Predictathon's own, reduced to one spelling per emoji by ReactionCatalogue.Canonicalise.
    -- This, not ReactionName, is what reactions group and toggle on.
    [ReactionId] NVARCHAR(100) NOT NULL,
    [CreationDate] DATETIME NOT NULL , 
    CONSTRAINT [PK_MessageReaction] PRIMARY KEY NONCLUSTERED ([MessageReactionID] ASC),
    CONSTRAINT [FK_MessageReaction_Message] FOREIGN KEY ([MessageID]) REFERENCES [dbo].[Message] ([MessageID]),
    CONSTRAINT [FK_MessageReaction_User] FOREIGN KEY ([UserID]) REFERENCES [Identity].[Users] ([Id])
);

GO
CREATE INDEX [IX_MessageReaction_MessageID] ON [dbo].[MessageReaction] ([MessageID] ASC, [CreationDate] ASC);
GO
CREATE NONCLUSTERED INDEX [IX_MessageReaction_UserID] ON [dbo].[MessageReaction] ([UserID] ASC);
GO
-- One reaction per user per identity per message. The service still checks before inserting, but
-- that check-then-insert is a race under concurrent requests, so this is what actually guarantees
-- it; AddReactionAsync treats the resulting DuplicateKeyException as "already reacted".
CREATE UNIQUE INDEX [IX_MessageReaction_MessageID_UserID_ReactionId] ON [dbo].[MessageReaction] ([MessageID] ASC, [UserID] ASC, [ReactionId] ASC);

