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
	SET NOCOUNT ON;
	
	SELECT
		[User].UserName AS Username
		, ulh.[Date]
		, ulh.Score
		, ulh.LeaguePosition
	FROM
		[Identity].[Users] AS [User]
		INNER JOIN [dbo].[UserCompetition] AS uc ON [User].Id = uc.UserID
		INNER JOIN [dbo].[UserCompetitionLeagueHistory] AS ulh ON uc.UserCompetitionID = ulh.UserCompetitionID
	WHERE
		uc.UserID = @UserID
		AND uc.CompetitionID = @CompetitionID
	ORDER BY
		ulh.[Date] ASC;
END;