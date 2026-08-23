/*
Reduces dbo.MessageReaction.ReactionId to one spelling per emoji.

Production carries two conventions for the same emoji: the legacy site stored Twemoji's filename
form ('u:2764'), while the React-era picker stores emoji-mart's padded, FE0F-retaining form
('u:2764-fe0f'). Both resolve to the same image, but reactions group and toggle on the identity
string - so the red heart was showing as two pills that couldn't toggle each other, with 56 more
emoji latently in the same state (see IReactionCatalogue.Canonicalise).

The canonical form is Twemoji's filename stem, which is what legacy rows already use. The mapping
below is generated from the vendored assets and the shipped emoji-mart dataset - it can't be
derived in T-SQL, because the filename rule has genuine exceptions (FE0F is usually dropped, but
kept on most ZWJ sequences, and not even consistently there).

Idempotent: rows already canonical don't match the mapping, and re-running merges nothing new.
*/

SET NOCOUNT ON;

IF COL_LENGTH('dbo.MessageReaction', 'ReactionId') IS NOT NULL
BEGIN
    DECLARE @Rewritten INT = 0;
    DECLARE @Merged INT = 0;

    DECLARE @Canonical TABLE ([From] NVARCHAR(100) NOT NULL PRIMARY KEY, [To] NVARCHAR(100) NOT NULL);

    INSERT INTO @Canonical ([From], [To])
    VALUES
    ('u:0023-fe0f-20e3', 'u:23-20e3'),
    ('u:002a-fe0f-20e3', 'u:2a-20e3'),
    ('u:0030-fe0f-20e3', 'u:30-20e3'),
    ('u:0031-fe0f-20e3', 'u:31-20e3'),
    ('u:0032-fe0f-20e3', 'u:32-20e3'),
    ('u:0033-fe0f-20e3', 'u:33-20e3'),
    ('u:0034-fe0f-20e3', 'u:34-20e3'),
    ('u:0035-fe0f-20e3', 'u:35-20e3'),
    ('u:0036-fe0f-20e3', 'u:36-20e3'),
    ('u:0037-fe0f-20e3', 'u:37-20e3'),
    ('u:0038-fe0f-20e3', 'u:38-20e3'),
    ('u:0039-fe0f-20e3', 'u:39-20e3'),
    ('u:00a9-fe0f', 'u:a9'),
    ('u:00ae-fe0f', 'u:ae'),
    ('u:1f170-fe0f', 'u:1f170'),
    ('u:1f171-fe0f', 'u:1f171'),
    ('u:1f17e-fe0f', 'u:1f17e'),
    ('u:1f17f-fe0f', 'u:1f17f'),
    ('u:1f202-fe0f', 'u:1f202'),
    ('u:1f237-fe0f', 'u:1f237'),
    ('u:1f321-fe0f', 'u:1f321'),
    ('u:1f324-fe0f', 'u:1f324'),
    ('u:1f325-fe0f', 'u:1f325'),
    ('u:1f326-fe0f', 'u:1f326'),
    ('u:1f327-fe0f', 'u:1f327'),
    ('u:1f328-fe0f', 'u:1f328'),
    ('u:1f329-fe0f', 'u:1f329'),
    ('u:1f32a-fe0f', 'u:1f32a'),
    ('u:1f32b-fe0f', 'u:1f32b'),
    ('u:1f32c-fe0f', 'u:1f32c'),
    ('u:1f336-fe0f', 'u:1f336'),
    ('u:1f37d-fe0f', 'u:1f37d'),
    ('u:1f396-fe0f', 'u:1f396'),
    ('u:1f397-fe0f', 'u:1f397'),
    ('u:1f399-fe0f', 'u:1f399'),
    ('u:1f39a-fe0f', 'u:1f39a'),
    ('u:1f39b-fe0f', 'u:1f39b'),
    ('u:1f39e-fe0f', 'u:1f39e'),
    ('u:1f39f-fe0f', 'u:1f39f'),
    ('u:1f3cb-fe0f', 'u:1f3cb'),
    ('u:1f3cc-fe0f', 'u:1f3cc'),
    ('u:1f3cd-fe0f', 'u:1f3cd'),
    ('u:1f3ce-fe0f', 'u:1f3ce'),
    ('u:1f3d4-fe0f', 'u:1f3d4'),
    ('u:1f3d5-fe0f', 'u:1f3d5'),
    ('u:1f3d6-fe0f', 'u:1f3d6'),
    ('u:1f3d7-fe0f', 'u:1f3d7'),
    ('u:1f3d8-fe0f', 'u:1f3d8'),
    ('u:1f3d9-fe0f', 'u:1f3d9'),
    ('u:1f3da-fe0f', 'u:1f3da'),
    ('u:1f3db-fe0f', 'u:1f3db'),
    ('u:1f3dc-fe0f', 'u:1f3dc'),
    ('u:1f3dd-fe0f', 'u:1f3dd'),
    ('u:1f3de-fe0f', 'u:1f3de'),
    ('u:1f3df-fe0f', 'u:1f3df'),
    ('u:1f3f3-fe0f', 'u:1f3f3'),
    ('u:1f3f5-fe0f', 'u:1f3f5'),
    ('u:1f3f7-fe0f', 'u:1f3f7'),
    ('u:1f43f-fe0f', 'u:1f43f'),
    ('u:1f441-fe0f', 'u:1f441'),
    ('u:1f441-fe0f-200d-1f5e8-fe0f', 'u:1f441-200d-1f5e8'),
    ('u:1f4fd-fe0f', 'u:1f4fd'),
    ('u:1f549-fe0f', 'u:1f549'),
    ('u:1f54a-fe0f', 'u:1f54a'),
    ('u:1f56f-fe0f', 'u:1f56f'),
    ('u:1f570-fe0f', 'u:1f570'),
    ('u:1f573-fe0f', 'u:1f573'),
    ('u:1f574-fe0f', 'u:1f574'),
    ('u:1f575-fe0f', 'u:1f575'),
    ('u:1f576-fe0f', 'u:1f576'),
    ('u:1f577-fe0f', 'u:1f577'),
    ('u:1f578-fe0f', 'u:1f578'),
    ('u:1f579-fe0f', 'u:1f579'),
    ('u:1f587-fe0f', 'u:1f587'),
    ('u:1f58a-fe0f', 'u:1f58a'),
    ('u:1f58b-fe0f', 'u:1f58b'),
    ('u:1f58c-fe0f', 'u:1f58c'),
    ('u:1f58d-fe0f', 'u:1f58d'),
    ('u:1f590-fe0f', 'u:1f590'),
    ('u:1f5a5-fe0f', 'u:1f5a5'),
    ('u:1f5a8-fe0f', 'u:1f5a8'),
    ('u:1f5b1-fe0f', 'u:1f5b1'),
    ('u:1f5b2-fe0f', 'u:1f5b2'),
    ('u:1f5bc-fe0f', 'u:1f5bc'),
    ('u:1f5c2-fe0f', 'u:1f5c2'),
    ('u:1f5c3-fe0f', 'u:1f5c3'),
    ('u:1f5c4-fe0f', 'u:1f5c4'),
    ('u:1f5d1-fe0f', 'u:1f5d1'),
    ('u:1f5d2-fe0f', 'u:1f5d2'),
    ('u:1f5d3-fe0f', 'u:1f5d3'),
    ('u:1f5dc-fe0f', 'u:1f5dc'),
    ('u:1f5dd-fe0f', 'u:1f5dd'),
    ('u:1f5de-fe0f', 'u:1f5de'),
    ('u:1f5e1-fe0f', 'u:1f5e1'),
    ('u:1f5e3-fe0f', 'u:1f5e3'),
    ('u:1f5e8-fe0f', 'u:1f5e8'),
    ('u:1f5ef-fe0f', 'u:1f5ef'),
    ('u:1f5f3-fe0f', 'u:1f5f3'),
    ('u:1f5fa-fe0f', 'u:1f5fa'),
    ('u:1f6cb-fe0f', 'u:1f6cb'),
    ('u:1f6cd-fe0f', 'u:1f6cd'),
    ('u:1f6ce-fe0f', 'u:1f6ce'),
    ('u:1f6cf-fe0f', 'u:1f6cf'),
    ('u:1f6e0-fe0f', 'u:1f6e0'),
    ('u:1f6e1-fe0f', 'u:1f6e1'),
    ('u:1f6e2-fe0f', 'u:1f6e2'),
    ('u:1f6e3-fe0f', 'u:1f6e3'),
    ('u:1f6e4-fe0f', 'u:1f6e4'),
    ('u:1f6e5-fe0f', 'u:1f6e5'),
    ('u:1f6e9-fe0f', 'u:1f6e9'),
    ('u:1f6f0-fe0f', 'u:1f6f0'),
    ('u:1f6f3-fe0f', 'u:1f6f3'),
    ('u:203c-fe0f', 'u:203c'),
    ('u:2049-fe0f', 'u:2049'),
    ('u:2122-fe0f', 'u:2122'),
    ('u:2139-fe0f', 'u:2139'),
    ('u:2194-fe0f', 'u:2194'),
    ('u:2195-fe0f', 'u:2195'),
    ('u:2196-fe0f', 'u:2196'),
    ('u:2197-fe0f', 'u:2197'),
    ('u:2198-fe0f', 'u:2198'),
    ('u:2199-fe0f', 'u:2199'),
    ('u:21a9-fe0f', 'u:21a9'),
    ('u:21aa-fe0f', 'u:21aa'),
    ('u:2328-fe0f', 'u:2328'),
    ('u:23cf-fe0f', 'u:23cf'),
    ('u:23ed-fe0f', 'u:23ed'),
    ('u:23ee-fe0f', 'u:23ee'),
    ('u:23ef-fe0f', 'u:23ef'),
    ('u:23f1-fe0f', 'u:23f1'),
    ('u:23f2-fe0f', 'u:23f2'),
    ('u:23f8-fe0f', 'u:23f8'),
    ('u:23f9-fe0f', 'u:23f9'),
    ('u:23fa-fe0f', 'u:23fa'),
    ('u:24c2-fe0f', 'u:24c2'),
    ('u:25aa-fe0f', 'u:25aa'),
    ('u:25ab-fe0f', 'u:25ab'),
    ('u:25b6-fe0f', 'u:25b6'),
    ('u:25c0-fe0f', 'u:25c0'),
    ('u:25fb-fe0f', 'u:25fb'),
    ('u:25fc-fe0f', 'u:25fc'),
    ('u:2600-fe0f', 'u:2600'),
    ('u:2601-fe0f', 'u:2601'),
    ('u:2602-fe0f', 'u:2602'),
    ('u:2603-fe0f', 'u:2603'),
    ('u:2604-fe0f', 'u:2604'),
    ('u:260e-fe0f', 'u:260e'),
    ('u:2611-fe0f', 'u:2611'),
    ('u:2618-fe0f', 'u:2618'),
    ('u:261d-fe0f', 'u:261d'),
    ('u:2620-fe0f', 'u:2620'),
    ('u:2622-fe0f', 'u:2622'),
    ('u:2623-fe0f', 'u:2623'),
    ('u:2626-fe0f', 'u:2626'),
    ('u:262a-fe0f', 'u:262a'),
    ('u:262e-fe0f', 'u:262e'),
    ('u:262f-fe0f', 'u:262f'),
    ('u:2638-fe0f', 'u:2638'),
    ('u:2639-fe0f', 'u:2639'),
    ('u:263a-fe0f', 'u:263a'),
    ('u:2640-fe0f', 'u:2640'),
    ('u:2642-fe0f', 'u:2642'),
    ('u:265f-fe0f', 'u:265f'),
    ('u:2660-fe0f', 'u:2660'),
    ('u:2663-fe0f', 'u:2663'),
    ('u:2665-fe0f', 'u:2665'),
    ('u:2666-fe0f', 'u:2666'),
    ('u:2668-fe0f', 'u:2668'),
    ('u:267b-fe0f', 'u:267b'),
    ('u:267e-fe0f', 'u:267e'),
    ('u:2692-fe0f', 'u:2692'),
    ('u:2694-fe0f', 'u:2694'),
    ('u:2695-fe0f', 'u:2695'),
    ('u:2696-fe0f', 'u:2696'),
    ('u:2697-fe0f', 'u:2697'),
    ('u:2699-fe0f', 'u:2699'),
    ('u:269b-fe0f', 'u:269b'),
    ('u:269c-fe0f', 'u:269c'),
    ('u:26a0-fe0f', 'u:26a0'),
    ('u:26a7-fe0f', 'u:26a7'),
    ('u:26b0-fe0f', 'u:26b0'),
    ('u:26b1-fe0f', 'u:26b1'),
    ('u:26c8-fe0f', 'u:26c8'),
    ('u:26cf-fe0f', 'u:26cf'),
    ('u:26d1-fe0f', 'u:26d1'),
    ('u:26d3-fe0f', 'u:26d3'),
    ('u:26e9-fe0f', 'u:26e9'),
    ('u:26f0-fe0f', 'u:26f0'),
    ('u:26f1-fe0f', 'u:26f1'),
    ('u:26f4-fe0f', 'u:26f4'),
    ('u:26f7-fe0f', 'u:26f7'),
    ('u:26f8-fe0f', 'u:26f8'),
    ('u:26f9-fe0f', 'u:26f9'),
    ('u:2702-fe0f', 'u:2702'),
    ('u:2708-fe0f', 'u:2708'),
    ('u:2709-fe0f', 'u:2709'),
    ('u:270c-fe0f', 'u:270c'),
    ('u:270d-fe0f', 'u:270d'),
    ('u:270f-fe0f', 'u:270f'),
    ('u:2712-fe0f', 'u:2712'),
    ('u:2714-fe0f', 'u:2714'),
    ('u:2716-fe0f', 'u:2716'),
    ('u:271d-fe0f', 'u:271d'),
    ('u:2721-fe0f', 'u:2721'),
    ('u:2733-fe0f', 'u:2733'),
    ('u:2734-fe0f', 'u:2734'),
    ('u:2744-fe0f', 'u:2744'),
    ('u:2747-fe0f', 'u:2747'),
    ('u:2763-fe0f', 'u:2763'),
    ('u:2764-fe0f', 'u:2764'),
    ('u:27a1-fe0f', 'u:27a1'),
    ('u:2934-fe0f', 'u:2934'),
    ('u:2935-fe0f', 'u:2935'),
    ('u:2b05-fe0f', 'u:2b05'),
    ('u:2b06-fe0f', 'u:2b06'),
    ('u:2b07-fe0f', 'u:2b07'),
    ('u:3030-fe0f', 'u:3030'),
    ('u:303d-fe0f', 'u:303d'),
    ('u:3297-fe0f', 'u:3297'),
    ('u:3299-fe0f', 'u:3299');

    UPDATE r
    SET [ReactionId] = c.[To]
    FROM [dbo].[MessageReaction] r
    INNER JOIN @Canonical c ON c.[From] = r.[ReactionId];

    SET @Rewritten = @@ROWCOUNT;

    -- Rewriting can collide a user's two spellings of one emoji on the same message. Keep the
    -- earliest, exactly as the UK-flag merge in 01_BackfillMessageReactionIdentity.sql does.
    WITH [Duplicates] AS
    (
        SELECT
            [MessageReactionID],
            ROW_NUMBER() OVER (
                PARTITION BY [MessageID], [UserID], [ReactionId]
                ORDER BY [CreationDate], [MessageReactionID]) AS [RowNumber]
        FROM [dbo].[MessageReaction]
        WHERE [ReactionId] IS NOT NULL
    )
    DELETE FROM [dbo].[MessageReaction]
    WHERE [MessageReactionID] IN (SELECT [MessageReactionID] FROM [Duplicates] WHERE [RowNumber] > 1);

    SET @Merged = @@ROWCOUNT;

    PRINT CONCAT(
        'MessageReaction identity canonicalisation: ', @Rewritten, ' rewritten, ',
        @Merged, ' duplicate(s) merged.');
END
