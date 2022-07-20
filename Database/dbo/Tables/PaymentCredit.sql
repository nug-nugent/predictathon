CREATE TABLE [dbo].[PaymentCredit] (
    [PaymentCreditID]   UNIQUEIDENTIFIER NOT NULL,
    [ExpectedUsername]  VARCHAR (50)     COLLATE Latin1_General_CI_AI NULL,
    [ForCompetitionID]  UNIQUEIDENTIFIER NOT NULL,
    [UniquePaymentCode] VARCHAR (10)     COLLATE Latin1_General_CI_AI NOT NULL,
    [CreditUsed]        BIT              CONSTRAINT [DF_PaymentCredit_CreditUsed] DEFAULT ((0)) NOT NULL,
    [UsedByUserID]      UNIQUEIDENTIFIER NULL,
    [IssuedByUserID]    UNIQUEIDENTIFIER NOT NULL,
    [IssueDate]         DATETIME         NOT NULL,
    CONSTRAINT [PK_PaymentCredit] PRIMARY KEY CLUSTERED ([PaymentCreditID] ASC),
    CONSTRAINT [FK_PaymentCredit_Competition] FOREIGN KEY ([ForCompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_PaymentCredit_IssuedByUser] FOREIGN KEY ([IssuedByUserID]) REFERENCES [dbo].[User] ([UserID]),
    CONSTRAINT [FK_PaymentCredit_UsedByUser] FOREIGN KEY ([UsedByUserID]) REFERENCES [dbo].[User] ([UserID])
);


GO
ALTER TABLE [dbo].[PaymentCredit] NOCHECK CONSTRAINT [FK_PaymentCredit_Competition];


GO
ALTER TABLE [dbo].[PaymentCredit] NOCHECK CONSTRAINT [FK_PaymentCredit_IssuedByUser];


GO
ALTER TABLE [dbo].[PaymentCredit] NOCHECK CONSTRAINT [FK_PaymentCredit_UsedByUser];

