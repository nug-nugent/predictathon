CREATE TABLE [dbo].[UserCompetition] (
    [UserCompetitionID]     UNIQUEIDENTIFIER NOT NULL,
    [UserID]                UNIQUEIDENTIFIER NOT NULL,
    [CompetitionID]         UNIQUEIDENTIFIER NOT NULL,
    [AmountPaid]            DECIMAL (5, 2)   CONSTRAINT [DF_UserCompetition_AmountPaid] DEFAULT ((0.00)) NOT NULL,
    [PaymentProvider]       VARCHAR (20)     NULL,
    [PaymentCreditID]       UNIQUEIDENTIFIER NULL,
    [IsDefaultCompetition]  BIT              CONSTRAINT [DF_UserCompetition_Default] DEFAULT ((0)) NOT NULL,
    [LastEmailReminderSent] DATETIME         NULL,
    CONSTRAINT [PK_UserCompetition] PRIMARY KEY CLUSTERED ([UserCompetitionID] ASC),
    CONSTRAINT [FK_UserCompetition_Competition] FOREIGN KEY ([CompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_UserCompetition_User] FOREIGN KEY ([UserID]) REFERENCES [Identity].[Users] ([Id])
);

