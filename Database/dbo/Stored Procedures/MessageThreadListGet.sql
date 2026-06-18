/* ==========================================================================================
	Description
		Returns all message threads
	
	History
		24/12/2011 : DH - Created
		15/06/2026 : DH - Added CTE for performance
		17/06/2026 : DH - Added local variables to minimise parameter sniffing issues
========================================================================================== */
CREATE PROCEDURE [dbo].[MessageThreadListGet]
	@UserLastViewedMessageboard DATETIME,
	@MessageThreadsReadThisSession UniqueIDAndDateTime READONLY,
	@IncludeHiddenFromPublic BIT = 0
AS
BEGIN
	SET NOCOUNT ON;
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	-- Minimise the effect of parameter sniffing:
	DECLARE @pUserLastViewedMessageboard DATETIME = @UserLastViewedMessageBoard;
	
	DECLARE @pMessageThreadsReadThisSession UniqueIDAndDateTime;
	INSERT
		@pMessageThreadsReadThisSession (UniqueID, DateAndTime)
	SELECT
		UniqueID
		, DateAndTime
	FROM
		@MessageThreadsReadThisSession;

	DECLARE @pIncludeHiddenFromPublic BIT = @IncludeHiddenFromPublic;

	;WITH MessageCounts AS (
		-- aggregate per thread: total messages and last message datetime
		SELECT
			m.MessageThreadID
			, COUNT(m.MessageID) AS MessageCount
			, MAX(m.MessageDateTime) AS LastMessageDate
		FROM 
			[Message] m
		GROUP BY 
			m.MessageThreadID
	),
	LastMessages AS (
		-- most recent message per thread
		SELECT
			m.MessageThreadID
			, m.MessageID
			, m.MessageDateTime
			, m.MessageContent
			, m.PostedByUserID
			, ROW_NUMBER() OVER (PARTITION BY m.MessageThreadID ORDER BY m.MessageDateTime DESC, m.MessageID DESC) AS rn
		FROM 
			[Message] m
	),
	LastMessageData AS (
		SELECT
			lm.MessageThreadID,
			lm.MessageContent,
			lm.MessageDateTime,
			u.Username AS LastPostedByUsername
		FROM 
			LastMessages lm
			LEFT JOIN [User] u ON lm.PostedByUserID = u.UserID
		WHERE 
			lm.rn = 1
	)

	SELECT
		mt.MessageThreadID
		, mt.ThreadSubject
		, StartedByUser = su.Username
		, mt.StartedByUserID
		, mt.StartedDateTime
		, LastMessageDate = mc.LastMessageDate
		, LastMessagePostedByUser = lmd.LastPostedByUsername
		, LastMessage = CASE WHEN lmd.MessageContent IS NOT NULL AND LEN(lmd.MessageContent) > 40 THEN LEFT(lmd.MessageContent, 37) + '...' ELSE lmd.MessageContent END
		, ReplyCount = COALESCE(mc.MessageCount, 0) - 1
		, Unread = CASE
			WHEN rt.DateAndTime IS NOT NULL THEN CASE WHEN rt.DateAndTime < mc.LastMessageDate THEN 1 ELSE 0 END
			ELSE CASE WHEN @pUserLastViewedMessageboard < mc.LastMessageDate THEN 1 ELSE 0 END
		END
	FROM 
		MessageThread mt
		LEFT JOIN MessageCounts mc ON mt.MessageThreadID = mc.MessageThreadID
		LEFT JOIN LastMessageData lmd ON mt.MessageThreadID = lmd.MessageThreadID
		LEFT JOIN [User] su ON mt.StartedByUserID = su.UserID
		LEFT JOIN @pMessageThreadsReadThisSession rt ON mt.MessageThreadID = rt.UniqueID
	WHERE 
		(@pIncludeHiddenFromPublic = CAST(1 AS BIT) OR mt.HiddenFromPublic = 0)
	ORDER BY
		LastMessageDate DESC;
END;