-- =============================================
-- Author:		David Huggett
-- Create date: 24/12/11
-- Description:	Returns all message threads
-- =============================================
CREATE PROCEDURE [dbo].[MessageThreadListGet]
	@UserLastViewedMessageboard DATETIME
	, @MessageThreadsReadThisSession UniqueIDAndDateTime READONLY
	, @IncludeHiddenFromPublic BIT = 0
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT
		MessageThread.MessageThreadID
		, MessageThread.ThreadSubject
		, StartedByUser = StartedByUser.Username
		, MessageThread.StartedByUserID
		, MessageThread.StartedDateTime
		, LastMessageDate = MAX([Message].MessageDateTime)
		, LastMessagePostedByUser = (SELECT TOP 1 
											LastPostedByUser.Username
										FROM 
											[Message] LastPostedMessage
											INNER JOIN [User] LastPostedByUser ON LastPostedMessage.PostedByUserID = LastPostedByUser.UserID
										WHERE 
											LastPostedMessage.MessageThreadID = MessageThread.MessageThreadID
										ORDER BY
											LastPostedMessage.MessageDateTime DESC)
		, LastMessage = (SELECT TOP 1 
								MessageContent = CASE WHEN LEN(LastPostedMessage.MessageContent) > 50 THEN 
														LEFT(LastPostedMessage.MessageContent, 50) + '...' 
													ELSE 
														LastPostedMessage.MessageContent 
													END
							FROM 
								[Message] LastPostedMessage
							WHERE 
								LastPostedMessage.MessageThreadID = MessageThread.MessageThreadID
							ORDER BY
								LastPostedMessage.MessageDateTime DESC)
		, ReplyCount = COUNT([Message].MessageID) - 1
		, Unread = CASE WHEN ReadThreads.DateAndTime IS NOT NULL THEN 
						CASE WHEN ReadThreads.DateAndTime <= MAX([Message].MessageDateTime) THEN 1 ELSE 0 END
					ELSE
						CASE WHEN @UserLastViewedMessageboard <= MAX([Message].MessageDateTime) THEN 1 ELSE 0 END
					END
	FROM
		MessageThread
		LEFT JOIN [Message] ON MessageThread.MessageThreadID = [Message].MessageThreadID
		LEFT JOIN [User] StartedByUser ON MessageThread.StartedByUserID = StartedByUser.UserID
		LEFT JOIN @MessageThreadsReadThisSession ReadThreads ON MessageThread.MessageThreadID = ReadThreads.UniqueID
	WHERE
		(ISNULL(@IncludeHiddenFromPublic, 0) = 1 OR MessageThread.HiddenFromPublic = 0)
	GROUP BY
		MessageThread.MessageThreadID
		, MessageThread.ThreadSubject
		, MessageThread.StartedByUserID
		, MessageThread.StartedDateTime	
		, StartedByUser.Username
		, ReadThreads.DateAndTime
	ORDER BY
		LastMessageDate DESC
END
