-- The running score for a match that has kicked off but has no confirmed result yet, from the
-- external data provider or entered by a match administrator. Deliberately separate from
-- Match.HomeTeamGoals/AwayTeamGoals, which stay the *confirmed* result: a live score is provisional,
-- carries no scoring weight, and must never be mistaken for one that has been processed.
--
-- One row per match (the primary key does the work of a unique constraint), created on the first
-- score we hear about and left in place afterwards - it costs nothing and keeps the full-time
-- scoreline visible on the Live page until an admin processes the result.
--
-- Status holds the provider's own value (IN_PLAY, PAUSED, FINISHED, ...) rather than an enum of our
-- own: it's provider vocabulary, we only branch on FINISHED, and inventing a mapping would just be
-- something else to keep in step. NULL when the row was only ever written by an admin.
--
-- UpdatedDateTime moves only when the scoreline actually changes, so it's the honest "as at" to show
-- a reader. LastPolledDateTime moves on every successful fetch whether the score changed or not, and
-- exists so a second worker process (IIS overlaps them briefly during a recycle) can see that
-- someone else has just polled and skip its turn.
CREATE TABLE [dbo].[MatchLiveScore] (
    [MatchID]            UNIQUEIDENTIFIER NOT NULL,
    [HomeTeamGoals]      INT              NOT NULL,
    [AwayTeamGoals]      INT              NOT NULL,
    [Status]             VARCHAR (20)     NULL,
    [Source]             VARCHAR (10)     NOT NULL,
    [UpdatedDateTime]    DATETIME         CONSTRAINT [DF_MatchLiveScore_UpdatedDateTime] DEFAULT (getdate()) NOT NULL,
    [LastPolledDateTime] DATETIME         NULL,
    [UpdatedByUserID]    UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_MatchLiveScore] PRIMARY KEY CLUSTERED ([MatchID] ASC),
    CONSTRAINT [FK_MatchLiveScore_Match_MatchID] FOREIGN KEY ([MatchID]) REFERENCES [dbo].[Match] ([MatchID]),
    CONSTRAINT [FK_MatchLiveScore_Users_UpdatedByUserID] FOREIGN KEY ([UpdatedByUserID]) REFERENCES [Identity].[Users] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_MatchLiveScore_UpdatedByUserID] ON [dbo].[MatchLiveScore]
(
	[UpdatedByUserID] ASC
);
GO
