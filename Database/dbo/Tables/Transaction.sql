CREATE TABLE [dbo].[Transaction] (
    [TransactionID]        UNIQUEIDENTIFIER NOT NULL,
    [CompetitionID]        UNIQUEIDENTIFIER NULL,
    [UserID]               UNIQUEIDENTIFIER NULL,
    [UserCompetitionID]    UNIQUEIDENTIFIER NULL,
    [Username]             VARCHAR (50)     COLLATE Latin1_General_CI_AI NULL,
    [Password]             VARCHAR (25)     COLLATE Latin1_General_CI_AI NULL,
    [EmailAddress]         VARCHAR (128)    COLLATE Latin1_General_CI_AI NULL,
    [Forenames]            VARCHAR (50)     COLLATE Latin1_General_CI_AI NULL,
    [Surname]              VARCHAR (50)     COLLATE Latin1_General_CI_AI NULL,
    [Amount]               DECIMAL (5, 2)   CONSTRAINT [DF_Transaction_Amount] DEFAULT ((0.00)) NOT NULL,
    [TransactionStatus]    VARCHAR (50)     COLLATE Latin1_General_CI_AI NULL,
    [ActualPaymentAmount]  DECIMAL (5, 2)   CONSTRAINT [DF_Transaction_ActualPaymentAmount] DEFAULT ((0.00)) NULL,
    [PayPalTransactionID]  CHAR (19)        COLLATE Latin1_General_CI_AI NULL,
    [PayPalFee]            DECIMAL (5, 2)   NULL,
    [TransactionDateTime]  DATETIME         NOT NULL,
    [Failed]               BIT              NULL,
    [ContainedInvalidData] BIT              NULL,
    [Comments]             VARCHAR (100)    COLLATE Latin1_General_CI_AI NULL,
    CONSTRAINT [PK_Transaction] PRIMARY KEY CLUSTERED ([TransactionID] ASC),
    CONSTRAINT [FK_Transaction_Competition] FOREIGN KEY ([CompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_Transaction_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]),
    CONSTRAINT [FK_Transaction_UserCompetition1] FOREIGN KEY ([UserCompetitionID]) REFERENCES [dbo].[UserCompetition] ([UserCompetitionID])
);


GO
ALTER TABLE [dbo].[Transaction] NOCHECK CONSTRAINT [FK_Transaction_Competition];


GO
ALTER TABLE [dbo].[Transaction] NOCHECK CONSTRAINT [FK_Transaction_User];


GO
ALTER TABLE [dbo].[Transaction] NOCHECK CONSTRAINT [FK_Transaction_UserCompetition1];

