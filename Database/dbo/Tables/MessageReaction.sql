CREATE TABLE [dbo].[MessageReaction] (
    [MessageReactionID] UNIQUEIDENTIFIER NOT NULL,
    [MessageID] UNIQUEIDENTIFIER NOT NULL,
    [UserID] UNIQUEIDENTIFIER NOT NULL,
    [ReactionName] NVARCHAR(200) NOT NULL,
    -- Namespaced reaction identity - 'u:{unified}' for a standard emoji, 'c:{id}' for one of
    -- Predictathon's own. This, not ReactionName, is what reactions group and toggle on.
    [ReactionId] NVARCHAR(100) NULL,
    -- Retired in favour of ReactionId: the server now resolves the image from the identity via
    -- ReactionCatalogue instead of storing a client-supplied, environment-coupled URL. Left in
    -- place (and made nullable) for one release so the backfill in Post-Deployment has something
    -- to read and there's a way back; drop it in a follow-up once production looks right.
    [ImageUrl] NVARCHAR(500) NULL, 
    [CreationDate] DATETIME NOT NULL , 
    CONSTRAINT [PK_MessageReaction] PRIMARY KEY NONCLUSTERED ([MessageReactionID] ASC),
    CONSTRAINT [FK_MessageReaction_Message] FOREIGN KEY ([MessageID]) REFERENCES [dbo].[Message] ([MessageID]),
    CONSTRAINT [FK_MessageReaction_User] FOREIGN KEY ([UserID]) REFERENCES [Identity].[Users] ([Id])
);

GO
CREATE INDEX [IX_MessageReaction_MessageID] ON [dbo].[MessageReaction] ([MessageID] ASC, [CreationDate] ASC);
GO
CREATE NONCLUSTERED INDEX [IX_MessageReaction_UserID] ON [dbo].[MessageReaction] ([UserID] ASC);

