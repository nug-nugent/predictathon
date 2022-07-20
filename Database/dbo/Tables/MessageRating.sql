CREATE TABLE [dbo].[MessageRating] (
    [MessageRatingID] UNIQUEIDENTIFIER NOT NULL,
    [MessageID]       UNIQUEIDENTIFIER NOT NULL,
    [Rating]          INT              NOT NULL,
    [RatedByUserID]   UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_MessageRating] PRIMARY KEY NONCLUSTERED ([MessageRatingID] ASC),
    CONSTRAINT [FK_MessageRating_Message] FOREIGN KEY ([MessageID]) REFERENCES [dbo].[Message] ([MessageID]),
    CONSTRAINT [FK_MessageRating_User] FOREIGN KEY ([RatedByUserID]) REFERENCES [dbo].[User] ([UserID])
);


GO
ALTER TABLE [dbo].[MessageRating] NOCHECK CONSTRAINT [FK_MessageRating_Message];


GO
ALTER TABLE [dbo].[MessageRating] NOCHECK CONSTRAINT [FK_MessageRating_User];


GO
CREATE CLUSTERED INDEX [IX_MessageRating_MessageID]
    ON [dbo].[MessageRating]([MessageID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_MessageRating_RatedByUserID]
    ON [dbo].[MessageRating]([RatedByUserID] ASC);

