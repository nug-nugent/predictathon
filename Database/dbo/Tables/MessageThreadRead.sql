CREATE TABLE [dbo].[MessageThreadRead] (
    [MessageThreadReadID] INT IDENTITY (1, 1) NOT NULL,
    [UserID] UNIQUEIDENTIFIER NOT NULL,
    [MessageThreadID] UNIQUEIDENTIFIER NOT NULL,
    [LastReadDateTime] DATETIME NOT NULL,
    CONSTRAINT [PK_MessageThreadRead] PRIMARY KEY CLUSTERED ([MessageThreadReadID] ASC),
    CONSTRAINT [FK_MessageThreadRead_User] FOREIGN KEY ([UserID]) REFERENCES [Identity].[Users] ([Id]),
    CONSTRAINT [FK_MessageThreadRead_MessageThread] FOREIGN KEY ([MessageThreadID]) REFERENCES [dbo].[MessageThread] ([MessageThreadID])
);

GO
-- Unique (not just a supporting index): a concurrent double-call to MarkThreadReadAsync (e.g. React
-- StrictMode double-invoking an effect) can otherwise race past the service-layer check-then-upsert
-- and insert two rows for the same (UserID, MessageThreadID), which then fans out the thread list
-- query's join into duplicate rows. The service catches the resulting duplicate-key violation.
CREATE UNIQUE INDEX [IX_MessageThreadRead_UserID_MessageThreadID] ON [dbo].[MessageThreadRead] ([UserID] ASC, [MessageThreadID] ASC);

