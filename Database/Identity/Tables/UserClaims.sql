CREATE TABLE [Identity].[UserClaims] (
    [Id]         INT              IDENTITY (1, 1) NOT NULL,
    [UserId]     UNIQUEIDENTIFIER NOT NULL,
    [ClaimType]  NVARCHAR (MAX)   NULL,
    [ClaimValue] NVARCHAR (MAX)   NULL,
    CONSTRAINT [PK_UserClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Identity].[Users] ([Id]) ON DELETE CASCADE
);


GO
CREATE INDEX [IX_UserClaims_UserId]
    ON [Identity].[UserClaims]([UserId] ASC);


