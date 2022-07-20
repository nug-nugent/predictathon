CREATE TABLE [dbo].[PredictionHistory] (
    [PredictionHistoryID] INT              IDENTITY (1, 1) NOT NULL,
    [PredictionID]        UNIQUEIDENTIFIER NOT NULL,
    [HomeTeamGoals]       INT              NULL,
    [AwayTeamGoals]       INT              NULL,
    [PredictionDateTime]  DATETIME         CONSTRAINT [DF_PredictionHistory_PredictionDateTime] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_PredictionHistory] PRIMARY KEY CLUSTERED ([PredictionHistoryID] ASC)
);




GO
CREATE NONCLUSTERED INDEX [IX_PredictionHistory_PredictionID]
    ON [dbo].[PredictionHistory]([PredictionID] ASC);

