CREATE TABLE [dbo].[Transaction] (
    [TransactionID]        UNIQUEIDENTIFIER NOT NULL,
    [CompetitionID]        UNIQUEIDENTIFIER NULL,
    [UserID]               UNIQUEIDENTIFIER NULL,
    [UserCompetitionID]    UNIQUEIDENTIFIER NULL,
    [Username]             VARCHAR (50)     NULL,
    [Password]             VARCHAR (25)     NULL,
    [EmailAddress]         VARCHAR (128)    NULL,
    [Forenames]            VARCHAR (50)     NULL,
    [Surname]              VARCHAR (50)     NULL,
    [Amount]               DECIMAL (5, 2)   CONSTRAINT [DF_Transaction_Amount] DEFAULT ((0.00)) NOT NULL,
    [TransactionStatus]    VARCHAR (50)     NULL,
    [ActualPaymentAmount]  DECIMAL (5, 2)   CONSTRAINT [DF_Transaction_ActualPaymentAmount] DEFAULT ((0.00)) NULL,
    [PayPalTransactionID]  CHAR (19)        NULL,
    [PayPalFee]            DECIMAL (5, 2)   NULL,
    [TransactionDateTime]  DATETIME         NOT NULL,
    [Failed]               BIT              NULL,
    [ContainedInvalidData] BIT              NULL,
    [Comments]             VARCHAR (100)    NULL,
    CONSTRAINT [PK_Transaction] PRIMARY KEY CLUSTERED ([TransactionID] ASC),
    CONSTRAINT [FK_Transaction_Competition] FOREIGN KEY ([CompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_Transaction_User] FOREIGN KEY ([UserID]) REFERENCES [Identity].[Users] ([Id]),
    CONSTRAINT [FK_Transaction_UserCompetition1] FOREIGN KEY ([UserCompetitionID]) REFERENCES [dbo].[UserCompetition] ([UserCompetitionID])
);


GO
ALTER TABLE [dbo].[Transaction] NOCHECK CONSTRAINT [FK_Transaction_Competition];


GO
ALTER TABLE [dbo].[Transaction] NOCHECK CONSTRAINT [FK_Transaction_User];


GO
ALTER TABLE [dbo].[Transaction] NOCHECK CONSTRAINT [FK_Transaction_UserCompetition1];

