CREATE TABLE [Identity].[Roles] (
    [Id]               UNIQUEIDENTIFIER NOT NULL DEFAULT (newid()),
    [Name]             NVARCHAR (256)   NULL,
    [NormalizedName]   NVARCHAR (256)   NULL,
    [ConcurrencyStamp] NVARCHAR (MAX)   NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE INDEX [RoleNameIndex]
    ON [Identity].[Roles]([NormalizedName] ASC) WHERE ([NormalizedName] IS NOT NULL);


