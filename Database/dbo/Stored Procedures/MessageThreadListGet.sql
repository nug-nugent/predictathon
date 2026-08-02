/* ==========================================================================================
	Description
		Returns all message threads

	History
		24/12/2011 : DH - Created
		15/06/2026 : DH - Added CTE for performance
		17/06/2026 : DH - Added local variables to minimise parameter sniffing issues
		17/07/2026 : DH - Replaced the session-scoped @MessageThreadsReadThisSession table-valued
		                   parameter and the @UserLastViewedMessageboard fallback with a join against
		                   the new persisted dbo.MessageThreadRead table, keyed by @UserID.
========================================================================================== */
CREATE PROCEDURE [dbo].[MessageThreadListGet]
	@UserID UNIQUEIDENTIFIER
	, @IncludeHiddenFromPublic BIT = 0
AS
BEGIN
	SET NOCOUNT ON;
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Minimise the effect of parameter sniffing:
	DECLARE @pUserID UNIQUEIDENTIFIER = @UserID;
	DECLARE @pIncludeHiddenFromPublic BIT = @IncludeHiddenFromPublic;

	;WITH MessageCounts AS (
		-- aggregate per thread: total messages and last message datetime
		SELECT
			m.MessageThreadID
			, COUNT(m.MessageID) AS MessageCount
			, MAX(m.MessageDateTime) AS LastMessageDate
		FROM
			[dbo].[Message] AS m
		GROUP BY
			m.MessageThreadID
	),
	LastMessages AS (
		-- most recent message per thread
		SELECT
			m.MessageThreadID
			, m.PostedByUserID
			, ROW_NUMBER() OVER (PARTITION BY m.MessageThreadID ORDER BY m.MessageDateTime DESC, m.MessageID DESC) AS rn
		FROM
			[dbo].[Message] AS m
	),
	LastMessageData AS (
		SELECT
			lm.MessageThreadID,
			u.UserName AS LastPostedByUsername
		FROM
			LastMessages lm
			LEFT JOIN [Identity].[Users] u ON lm.PostedByUserID = u.Id
		WHERE
			lm.rn = 1
	)

	SELECT
		mt.MessageThreadID
		, mt.ThreadSubject
		, StartedByUsername = su.UserName
		, mt.StartedDateTime
		, mt.HiddenFromPublic
		, MessageCount = COALESCE(mc.MessageCount, 0)
		, LastMessageDateTime = COALESCE(mc.LastMessageDate, mt.StartedDateTime)
		, LastMessageByUsername = COALESCE(lmd.LastPostedByUsername, su.Username)
		, Unread = CASE
			WHEN rt.LastReadDateTime IS NULL THEN CAST(1 AS BIT)
			WHEN rt.LastReadDateTime < COALESCE(mc.LastMessageDate, mt.StartedDateTime) THEN CAST(1 AS BIT)
			ELSE CAST(0 AS BIT)
		END
	FROM
		[dbo].[MessageThread] AS mt
		LEFT JOIN MessageCounts mc ON mt.MessageThreadID = mc.MessageThreadID
		LEFT JOIN LastMessageData lmd ON mt.MessageThreadID = lmd.MessageThreadID
		LEFT JOIN [Identity].[Users] su ON mt.StartedByUserID = su.Id
		LEFT JOIN [dbo].[MessageThreadRead] AS rt ON rt.MessageThreadID = mt.MessageThreadID AND rt.UserID = @pUserID
	WHERE
		(@pIncludeHiddenFromPublic = CAST(1 AS BIT) OR mt.HiddenFromPublic = 0)
	ORDER BY
		LastMessageDateTime DESC;
END;