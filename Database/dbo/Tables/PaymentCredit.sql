CREATE TABLE [dbo].[PaymentCredit] (
    [PaymentCreditID]   UNIQUEIDENTIFIER NOT NULL,
    [ExpectedUsername]  VARCHAR (50)     NULL,
    [ForCompetitionID]  UNIQUEIDENTIFIER NOT NULL,
    [UniquePaymentCode] VARCHAR (10)     NOT NULL,
    [CreditUsed]        BIT              CONSTRAINT [DF_PaymentCredit_CreditUsed] DEFAULT ((0)) NOT NULL,
    [UsedByUserID]      UNIQUEIDENTIFIER NULL,
    [IssuedByUserID]    UNIQUEIDENTIFIER NOT NULL,
    [IssueDate]         DATETIME         NOT NULL,
    CONSTRAINT [PK_PaymentCredit] PRIMARY KEY CLUSTERED ([PaymentCreditID] ASC),
    CONSTRAINT [FK_PaymentCredit_Competition] FOREIGN KEY ([ForCompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_PaymentCredit_IssuedByUser] FOREIGN KEY ([IssuedByUserID]) REFERENCES [Identity].[Users] ([Id]),
    CONSTRAINT [FK_PaymentCredit_UsedByUser] FOREIGN KEY ([UsedByUserID]) REFERENCES [Identity].[Users] ([Id])
);


GO
ALTER TABLE [dbo].[PaymentCredit] NOCHECK CONSTRAINT [FK_PaymentCredit_Competition];


GO
ALTER TABLE [dbo].[PaymentCredit] NOCHECK CONSTRAINT [FK_PaymentCredit_IssuedByUser];


GO
ALTER TABLE [dbo].[PaymentCredit] NOCHECK CONSTRAINT [FK_PaymentCredit_UsedByUser];

