CREATE TABLE [Identity].[RoleClaims] (
    [Id]         INT              IDENTITY (1, 1) NOT NULL,
    [RoleId]     UNIQUEIDENTIFIER NOT NULL,
    [ClaimType]  NVARCHAR (MAX)   NULL,
    [ClaimValue] NVARCHAR (MAX)   NULL,
    CONSTRAINT [PK_RoleClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Identity].[Roles] ([Id]) ON DELETE CASCADE
);


GO
CREATE INDEX [IX_RoleClaims_RoleId]
    ON [Identity].[RoleClaims]([RoleId] ASC);


