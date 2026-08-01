CREATE TABLE [dbo].[Competition] (
    [CompetitionID]                    UNIQUEIDENTIFIER NOT NULL DEFAULT (newid()),
    [CompetitionName]                  VARCHAR (50)     NOT NULL,
    [PrependNameWithThe]               BIT              CONSTRAINT [DF_Competition_PrependWithThe] DEFAULT ((0)) NOT NULL,
    [StartDate]                        DATE             NOT NULL,
    [EndDate]                          DATE             NOT NULL,
    [DuplicateFixturesAllowed]         BIT              CONSTRAINT [DF_Competition_DuplicateFixturesAllowed] DEFAULT ((0)) NOT NULL,
    [OpenForRegistration]              BIT              CONSTRAINT [DF_Competition_OpenForRegistration] DEFAULT ((0)) NOT NULL,
    [RegistrationAvailableOnLoginPage] BIT              CONSTRAINT [DF_Competition_RegistrationAvailableOnLoginPage] DEFAULT ((0)) NOT NULL,
    [ShowInHallOfFame]                 BIT              CONSTRAINT [DF_Competition_ShowInHallOfFame] DEFAULT ((0)) NOT NULL,
    [EntranceFee]                      DECIMAL (5, 2)   CONSTRAINT [DF_Competition_EntranceFee] DEFAULT ((0.00)) NOT NULL,
    [PayPalPaymentAvailable]           BIT              CONSTRAINT [DF_Competition_PayPalPaymentAvailable] DEFAULT ((1)) NOT NULL,
    [Information]                      VARCHAR (MAX)    NULL,
    [ImageFilename]                    VARCHAR (40)     NULL,
    [DefaultToNeutralGround]           BIT              CONSTRAINT [DF_Competition_DefaultToNeutralGround] DEFAULT ((0)) NOT NULL,
    [AllowTwoPointers]                 BIT              CONSTRAINT [DF_Competition_AllowTwoPointers] DEFAULT ((1)) NOT NULL,
    [ExternalApiCompetitionCode]       VARCHAR (10)     NULL,
    CONSTRAINT [PK_Competition] PRIMARY KEY CLUSTERED ([CompetitionID] ASC)
);



