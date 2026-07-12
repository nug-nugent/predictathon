CREATE TABLE [Identity].[RefreshTokens] (
    [Id]            UNIQUEIDENTIFIER NOT NULL DEFAULT (newid()),
    [UserId]        UNIQUEIDENTIFIER NOT NULL,
    [TokenHash]     VARBINARY (32)   NOT NULL,
    [ExpiresAtUtc]  DATETIME2 (7)    NOT NULL,
    [CreatedAtUtc]  DATETIME2 (7)    CONSTRAINT [DF_RefreshTokens_CreatedAtUtc] DEFAULT (sysutcdatetime()) NOT NULL,
    [RevokedAtUtc]  DATETIME2 (7)    NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash]
    ON [Identity].[RefreshTokens]([TokenHash] ASC);


GO
CREATE INDEX [IX_RefreshTokens_UserId]
    ON [Identity].[RefreshTokens]([UserId] ASC);


