SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [Identity].[Users] (
    [Id]                          UNIQUEIDENTIFIER NOT NULL DEFAULT (newid()),
    [UserName]                    NVARCHAR (256)   NULL,
    [NormalizedUserName]          NVARCHAR (256)   NULL,
    [Email]                       NVARCHAR (256)   NULL,
    [NormalizedEmail]             NVARCHAR (256)   NULL,
    [EmailConfirmed]              BIT              CONSTRAINT [DF_Users_EmailConfirmed] DEFAULT ((0)) NOT NULL,
    [PasswordHash]                NVARCHAR (MAX)   NULL,
    [SecurityStamp]               NVARCHAR (MAX)   NULL,
    [ConcurrencyStamp]            NVARCHAR (MAX)   NULL,
    [PhoneNumber]                 NVARCHAR (MAX)   NULL,
    [PhoneNumberConfirmed]        BIT              CONSTRAINT [DF_Users_PhoneNumberConfirmed] DEFAULT ((0)) NOT NULL,
    [TwoFactorEnabled]            BIT              CONSTRAINT [DF_Users_TwoFactorEnabled] DEFAULT ((0)) NOT NULL,
    [LockoutEnd]                  DATETIMEOFFSET   NULL,
    [LockoutEnabled]              BIT              CONSTRAINT [DF_Users_LockoutEnabled] DEFAULT ((1)) NOT NULL,
    [AccessFailedCount]           INT              CONSTRAINT [DF_Users_AccessFailedCount] DEFAULT ((0)) NOT NULL,
    [Forenames]                   VARCHAR (50)     NULL,
    [Surname]                     VARCHAR (50)     NULL,
    [FavouriteTeam]               VARCHAR (50)     NULL,
    [Location]                    VARCHAR (50)     NULL,
    [Caption]                     VARCHAR (30)     NULL,
    [ProfileText]                 VARCHAR (MAX)    NULL,
    [LastLoginDateTime]           DATETIME         NULL,
    [ImageUploaded]               BIT              CONSTRAINT [DF_Users_ImageUploaded] DEFAULT ((0)) NOT NULL,
    [TotalMessageboardPosts]      INT              CONSTRAINT [DF_Users_TotalMessageboardPosts] DEFAULT ((0)) NOT NULL,
    [EmailPredictionReminderDays] INT              NULL,
    [CanViewHiddenMessageThreads] BIT              CONSTRAINT [DF_Users_CanViewHiddenMessageThreads] DEFAULT ((0)) NOT NULL,
    [CanViewMessageboard]         BIT              CONSTRAINT [DF_Users_CanViewMessageboard] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE INDEX [UserNameIndex]
    ON [Identity].[Users]([NormalizedUserName] ASC) WHERE ([NormalizedUserName] IS NOT NULL);


GO
CREATE INDEX [EmailIndex]
    ON [Identity].[Users]([NormalizedEmail] ASC);


