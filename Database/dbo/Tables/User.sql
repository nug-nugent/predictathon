CREATE TABLE [dbo].[User] (
    [UserID]                      UNIQUEIDENTIFIER NOT NULL,
    [Username]                    VARCHAR (50)     NOT NULL,
    [Password]                    VARCHAR (50)     NOT NULL,
    [EmailAddress]                VARCHAR (128)    NULL,
    [Forenames]                   VARCHAR (50)     NULL,
    [Surname]                     VARCHAR (50)     NULL,
    [FavouriteTeam]               VARCHAR (50)     NULL,
    [Location]                    VARCHAR (50)     NULL,
    [Caption]                     VARCHAR (30)     NULL,
    [ProfileText]                 VARCHAR (MAX)    NULL,
    [LastLoginDateTime]           DATETIME         NULL,
    [MatchAdministrator]          BIT              CONSTRAINT [DF_User_MatchAdministrator] DEFAULT ((0)) NOT NULL,
    [UserAdministrator]           BIT              CONSTRAINT [DF_User_UserAdministrator] DEFAULT ((0)) NOT NULL,
    [CompetitionAdministrator]    BIT              CONSTRAINT [DF_User_CompetitionAdministrator] DEFAULT ((0)) NOT NULL,
    [ImageUploaded]               BIT              CONSTRAINT [DF_User_ImageUploaded1] DEFAULT ((0)) NOT NULL,
    [LastViewedMessageboard]      DATETIME         NULL,
    [TotalMessageboardPosts]      INT              CONSTRAINT [DF_User_TotalMessageboardPosts] DEFAULT ((0)) NOT NULL,
    [EmailPredictionReminderDays] INT              NULL,
    [CanViewHiddenMessageThreads] BIT              CONSTRAINT [DF_User_CanViewHiddenMessageThreads] DEFAULT ((0)) NOT NULL,
    [CanViewMessageboard]         BIT              CONSTRAINT [DF_User_CanViewMessageboard] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([UserID] ASC)
);



