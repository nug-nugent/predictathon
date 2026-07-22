CREATE TABLE [Identity].[UserLogins] (
    [LoginProvider]       NVARCHAR (450)   NOT NULL,
    [ProviderKey]         NVARCHAR (450)   NOT NULL,
    [ProviderDisplayName] NVARCHAR (MAX)   NULL,
    [UserId]              UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_UserLogins] PRIMARY KEY CLUSTERED ([LoginProvider] ASC, [ProviderKey] ASC),
    CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users] ([Id]) ON DELETE CASCADE
);


GO
CREATE INDEX [IX_UserLogins_UserId]
    ON [Identity].[UserLogins]([UserId] ASC);


