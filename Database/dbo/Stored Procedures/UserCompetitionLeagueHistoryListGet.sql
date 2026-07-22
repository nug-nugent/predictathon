-- =============================================
-- Author:		David Huggett
-- Create date: 09/02/2012
-- Description:	Returns the league history for a given user and competition
-- =============================================
CREATE PROCEDURE [dbo].[UserCompetitionLeagueHistoryListGet]
	@UserID UNIQUEIDENTIFIER
	, @CompetitionID UNIQUEIDENTIFIER
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;
	
	SELECT
		[User].UserName AS Username
		, UserCompetitionLeagueHistory.[Date]
		, UserCompetitionLeagueHistory.Score
		, UserCompetitionLeagueHistory.LeaguePosition
	FROM
		[Identity].[Users] AS [User]
		INNER JOIN UserCompetition ON [User].Id = UserCompetition.UserID
		INNER JOIN UserCompetitionLeagueHistory ON UserCompetition.UserCompetitionID = UserCompetitionLeagueHistory.UserCompetitionID
	WHERE
		UserCompetition.UserID = @UserID
		AND UserCompetition.CompetitionID = @CompetitionID
	ORDER BY
		UserCompetitionLeagueHistory.[Date] ASC
END
