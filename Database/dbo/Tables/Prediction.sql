CREATE TABLE [dbo].[Prediction] (
    [PredictionID]                   UNIQUEIDENTIFIER NOT NULL,
    [MatchID]                        UNIQUEIDENTIFIER NOT NULL,
    [UserID]                         UNIQUEIDENTIFIER NOT NULL,
    [HomeTeamGoals]                  INT              NULL,
    [AwayTeamGoals]                  INT              NULL,
    [Score]                          INT              NULL,
    [GoalDifference]                 INT              NULL,
    [PredictionHistoryID]            INT              NULL,
    [Invalid]                        BIT              CONSTRAINT [DF_Prediction_Invalid] DEFAULT ((0)) NOT NULL,
    [AutoUpdatedDueToLatePrediction] BIT              CONSTRAINT [DF_Prediction_AutoUpdatedDueToLatePrediction] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Prediction] PRIMARY KEY CLUSTERED ([PredictionID] ASC),
    CONSTRAINT [FK_Prediction_Match] FOREIGN KEY ([MatchID]) REFERENCES [dbo].[Match] ([MatchID]),
    CONSTRAINT [FK_Prediction_User] FOREIGN KEY ([UserID]) REFERENCES [Identity].[Users] ([Id]),
    CONSTRAINT [UK_Prediction_MatchID_UserID] UNIQUE NONCLUSTERED ([MatchID] ASC, [UserID] ASC)
);


GO
ALTER TABLE [dbo].[Prediction] NOCHECK CONSTRAINT [FK_Prediction_Match];


GO
ALTER TABLE [dbo].[Prediction] NOCHECK CONSTRAINT [FK_Prediction_User];




GO
ALTER TABLE [dbo].[Prediction] NOCHECK CONSTRAINT [FK_Prediction_Match];


GO
ALTER TABLE [dbo].[Prediction] NOCHECK CONSTRAINT [FK_Prediction_User];


GO
CREATE NONCLUSTERED INDEX [IX_Prediction_UserID_Included]
    ON [dbo].[Prediction]([UserID] ASC)
    INCLUDE([MatchID], [Score], [GoalDifference]);


GO
CREATE NONCLUSTERED INDEX [IX_Prediction_MatchID_Included]
    ON [dbo].[Prediction]([MatchID] ASC, [UserID] ASC, [HomeTeamGoals] ASC, [AwayTeamGoals] ASC, [Score] ASC, [GoalDifference] ASC);

