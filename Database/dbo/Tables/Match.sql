CREATE TABLE [dbo].[Match] (
    [MatchID]       UNIQUEIDENTIFIER NOT NULL,
    [CompetitionID] UNIQUEIDENTIFIER NOT NULL,
    [MatchDateTime] DATETIME         NOT NULL,
    [HomeTeamID]    UNIQUEIDENTIFIER NULL,
    [AwayTeamID]    UNIQUEIDENTIFIER NULL,
    [MatchPlayed]   BIT              CONSTRAINT [DF_Match_MatchPlayed] DEFAULT ((0)) NOT NULL,
    [HomeTeamGoals] INT              NULL,
    [AwayTeamGoals] INT              NULL,
    [NeutralGround] BIT              CONSTRAINT [DF_Match_NeutralGround] DEFAULT ((0)) NOT NULL,
    [HomeTeamTBC]   VARCHAR (50)     NULL,
    [AwayTeamTBC]   VARCHAR (50)     NULL,
    [Description]   VARCHAR (50)     NULL,
    [Knockout]      BIT              CONSTRAINT [DF_Match_Knockout] DEFAULT ((0)) NOT NULL,
    [ExternalMatchID] INT            NULL,
    -- Which knockout round this match belongs to, NULL for a group or league match. Valued as the
    -- number of teams contesting the round (16, 8, 4, 2) so it sorts from the first round through to
    -- the final on its own. The third-place play-off isn't a round of the bracket tree at all and
    -- takes 3: not a power of two, so it can never collide with a real round, and it sorts between
    -- the semi-finals and the final, which is when it's played. Treat this as an ordering key that
    -- happens to use the team count, not as a team count to do arithmetic on.
    [KnockoutRound] INT              NULL,
    -- 1-based position in the drawn bracket, read top to bottom: slots 1..n/2 are the top half of
    -- the draw and n/2+1..n the bottom, which is what puts a match on the left or right of the view.
    -- Deliberately our own numbering rather than the official match number, which competitions
    -- assign by schedule and which therefore says nothing about bracket position.
    [BracketSlot]   INT              NULL,
    -- When this match's result was confirmed, and by whom. Read together, since NULL means two
    -- different things in the two columns:
    --
    --   ProcessedDateTime NULL                        - never processed, or processed before these
    --                                                   columns existed (nothing was recorded, and
    --                                                   there's no way to work it out after the fact).
    --   ProcessedDateTime set, ProcessedByUserID NULL - confirmed automatically from the external
    --                                                   provider's final score, with no admin involved.
    --   both set                                      - an admin confirmed it on the Process Results page.
    --
    -- Records who first confirmed the result, not who last touched it: correcting a score afterwards
    -- (MatchService.Update) leaves these alone, so "processed automatically" stays a true statement
    -- about how the result got here. Deliberately not surfaced anywhere in the UI - it's an audit
    -- trail for reading in the database, not something a predictor or an admin needs to see.
    --
    -- No default on ProcessedDateTime, unlike the other wall-clock columns: a match is inserted
    -- unplayed, so getdate() would date the result of a match nobody has scored yet.
    [ProcessedDateTime] DATETIME     NULL,
    [ProcessedByUserID] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_Match] PRIMARY KEY CLUSTERED ([MatchID] ASC),
    CONSTRAINT [FK_Match_AwayTeam] FOREIGN KEY ([AwayTeamID]) REFERENCES [dbo].[Team] ([TeamID]),
    CONSTRAINT [FK_Match_Competition] FOREIGN KEY ([CompetitionID]) REFERENCES [dbo].[Competition] ([CompetitionID]),
    CONSTRAINT [FK_Match_HomeTeam] FOREIGN KEY ([HomeTeamID]) REFERENCES [dbo].[Team] ([TeamID]),
    CONSTRAINT [FK_Match_Users_ProcessedByUserID] FOREIGN KEY ([ProcessedByUserID]) REFERENCES [Identity].[Users] ([Id])
);


GO
-- Links a Match to its external fixture-provider ID (e.g. football-data.org), so the fixture sync
-- job can detect reschedules by ExternalMatchID rather than fuzzy-matching on teams/date. Filtered
-- because most matches (older seasons, cup fixtures) have no external source and NULL shouldn't
-- collide under a unique constraint.
CREATE UNIQUE INDEX [IX_Match_ExternalMatchID] ON [dbo].[Match] ([ExternalMatchID] ASC) WHERE ([ExternalMatchID] IS NOT NULL);
GO

CREATE NONCLUSTERED INDEX [IX_Match_CompetitionID] ON [dbo].[Match]
(
	[CompetitionID] ASC
);
GO

CREATE NONCLUSTERED INDEX [IX_Match_HomeTeamID] ON [dbo].[Match]
(
	[HomeTeamID] ASC
);
GO

CREATE NONCLUSTERED INDEX [IX_Match_AwayTeamID] ON [dbo].[Match]
(
	[AwayTeamID] ASC
);
GO

CREATE NONCLUSTERED INDEX [IX_Match_ProcessedByUserID] ON [dbo].[Match]
(
	[ProcessedByUserID] ASC
);
GO

