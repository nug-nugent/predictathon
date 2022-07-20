-- =============================================
-- Author:		David Huggett
-- Create date: 28/12/11
-- Description:	Returns all messages for a particular MessageThread
-- =============================================
CREATE PROCEDURE [dbo].[MessageThreadMessageListGet]
	@MessageThreadID UNIQUEIDENTIFIER
	, @UserID UNIQUEIDENTIFIER
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT
		[Message].MessageID
		, [Message].MessageContent
		, [Message].MessageDateTime
		, [Message].HasLinkedImage
		, [Message].YouTubeVideoID
		, PostedByUserID = [User].UserID
		, PostedByUsername = [User].Username
		, PostedByUserTotalMessageboardPosts = [User].TotalMessageboardPosts
		, PostedByUserImageUploaded = [User].ImageUploaded
		, [Message].UserTotalMessageboardPosts
		, UserMessageRating.MessageRatingID
		, UserMessageRating.Rating
		, AverageRating = AVG(CAST(MessageRating.Rating AS DECIMAL(3, 2)))
		, NumberOfRatings = COUNT(MessageRating.MessageRatingID)
	FROM
		[Message]
		INNER JOIN [User] ON [Message].PostedByUserID = [User].UserID
		LEFT JOIN (SELECT MessageRatingID, MessageID, Rating FROM MessageRating WHERE RatedByUserID = @UserID) UserMessageRating ON [Message].MessageID = UserMessageRating.MessageID
		LEFT JOIN MessageRating ON [Message].MessageID = MessageRating.MessageID
	WHERE
		[Message].MessageThreadID = @MessageThreadID
	GROUP BY
		[Message].MessageID
		, [Message].MessageContent
		, [Message].MessageDateTime
		, [Message].HasLinkedImage
		, [Message].YouTubeVideoID
		, [User].UserID
		, [User].Username
		, [User].TotalMessageboardPosts
		, [User].ImageUploaded
		, [Message].UserTotalMessageboardPosts
		, UserMessageRating.MessageRatingID
		, UserMessageRating.Rating
	ORDER BY
		[Message].MessageDateTime ASC
END
