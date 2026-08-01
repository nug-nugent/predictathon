CREATE TABLE [dbo].[FixtureChangeProposal] (
    [FixtureChangeProposalID] INT      IDENTITY (1, 1) NOT NULL,
    [MatchID]                 UNIQUEIDENTIFIER          NOT NULL,
    [PreviousMatchDateTime]   DATETIME                  NOT NULL,
    [ProposedMatchDateTime]   DATETIME                  NOT NULL,
    [DetectedAtUtc]           DATETIME CONSTRAINT [DF_FixtureChangeProposal_DetectedAtUtc] DEFAULT (sysutcdatetime()) NOT NULL,
    [Status]                  VARCHAR (20) CONSTRAINT [DF_FixtureChangeProposal_Status] DEFAULT ('Pending') NOT NULL,
    [ResolvedAtUtc]           DATETIME                  NULL,
    CONSTRAINT [PK_FixtureChangeProposal] PRIMARY KEY CLUSTERED ([FixtureChangeProposalID] ASC),
    CONSTRAINT [FK_FixtureChangeProposal_Match] FOREIGN KEY ([MatchID]) REFERENCES [dbo].[Match] ([MatchID])
);


GO
-- Stops the sync job creating a second pending proposal for a match that already has one
-- outstanding - Confirmed/Dismissed rows are excluded so a match can accumulate history over time.
CREATE UNIQUE INDEX [IX_FixtureChangeProposal_MatchID_Pending] ON [dbo].[FixtureChangeProposal] ([MatchID] ASC) WHERE ([Status] = 'Pending');
