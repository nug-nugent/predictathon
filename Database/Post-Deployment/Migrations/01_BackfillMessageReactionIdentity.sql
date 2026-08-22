/*
Backfills dbo.MessageReaction.ReactionId for rows created before reactions were keyed on a stable
identity rather than on a client-supplied image URL.

The old ImageUrl already contains the identity we want - the filename is the emoji-mart "unified"
codepoint sequence (including the ones whose URL was broken, e.g. ".../2764-fe0f.svg": the URL was
dead but "2764-fe0f" is exactly the right identity, and ReactionCatalogue now maps it onto the
"2764.svg" that Twemoji actually ships).

Custom reactions map by filename onto their manifest id. The three UK subdivision flags are
deliberately NOT in that mapping: they used to be duplicated as custom entries *and* be pickable
from the standard Flags category, producing two separate pills for the same flag. Letting them
fall through to the 'u:' branch merges both spellings onto the one Unicode identity.

Idempotent: only touches rows whose ReactionId is still NULL.
*/

SET NOCOUNT ON;

IF COL_LENGTH('dbo.MessageReaction', 'ReactionId') IS NOT NULL
   AND COL_LENGTH('dbo.MessageReaction', 'ImageUrl') IS NOT NULL
BEGIN
    DECLARE @Backfilled INT = 0;
    DECLARE @Merged INT = 0;
    DECLARE @Stragglers INT = 0;

    WITH [Parsed] AS
    (
        SELECT
            [MessageReactionID],
            -- Everything after the final '/' - the URL's origin/prefix varied by environment,
            -- which is precisely why it isn't stored any more.
            CASE
                WHEN CHARINDEX('/', REVERSE([ImageUrl])) > 0
                    THEN RIGHT([ImageUrl], CHARINDEX('/', REVERSE([ImageUrl])) - 1)
                ELSE [ImageUrl]
            END AS [FileName]
        FROM [dbo].[MessageReaction]
        WHERE [ReactionId] IS NULL
          AND [ImageUrl] IS NOT NULL
    )
    UPDATE r
    SET [ReactionId] =
        CASE p.[FileName]
            WHEN 'brewdog.png'      THEN 'c:brewdog'
            WHEN 'guinness.png'     THEN 'c:guinness'
            WHEN 'ludo.png'         THEN 'c:ludo'
            WHEN 'red-card.png'     THEN 'c:red_card'
            WHEN 'yellow-card.png'  THEN 'c:yellow_card'
            WHEN 'pt.png'           THEN 'c:pussy_time'
            WHEN 'beaker.png'       THEN 'c:beaker'
            WHEN 'facepalm.png'     THEN 'c:facepalm'
            WHEN 'rick.png'         THEN 'c:rick_roll'
            WHEN 'success.png'      THEN 'c:success_boy'
            WHEN 'vaultboy.jpg'     THEN 'c:vault_boy'
            WHEN 'woo.png'          THEN 'c:woo_hoo'
            ELSE 'u:' + LOWER(
                CASE
                    WHEN CHARINDEX('.', REVERSE(p.[FileName])) > 0
                        THEN LEFT(p.[FileName], LEN(p.[FileName]) - CHARINDEX('.', REVERSE(p.[FileName])))
                    ELSE p.[FileName]
                END)
        END
    FROM [dbo].[MessageReaction] r
    INNER JOIN [Parsed] p ON p.[MessageReactionID] = r.[MessageReactionID];

    SET @Backfilled = @@ROWCOUNT;

    -- The flag merge above can leave one user holding the same identity twice on a message (once
    -- picked as a custom entry, once from the standard Flags category). Keep the earliest.
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

    SELECT @Stragglers = COUNT(*) FROM [dbo].[MessageReaction] WHERE [ReactionId] IS NULL;

    PRINT CONCAT(
        'MessageReaction identity backfill: ', @Backfilled, ' backfilled, ',
        @Merged, ' duplicate(s) merged, ', @Stragglers, ' row(s) still without a ReactionId.');
END
