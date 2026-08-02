-- =============================================
-- Author:		David Huggett
-- Create date: 10/01/12
-- Description:	Returns a list of teams for a given competition
-- =============================================
CREATE PROCEDURE [dbo].[TeamCompetitionListGet]
	@CompetitionID UNIQUEIDENTIFIER
	, @ReturnTeamsNotInCompetition BIT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT
		TeamCompetition.TeamCompetitionID
		, t.TeamID
		, t.TeamName
	FROM
		[dbo].[Team] AS t
		LEFT JOIN (SELECT tc.TeamCompetitionID, tc.TeamID, tc.CompetitionID FROM [dbo].[TeamCompetition] AS tc WHERE tc.CompetitionID = @CompetitionID) TeamCompetition ON TeamCompetition.TeamID = t.TeamID
	WHERE
		((@ReturnTeamsNotInCompetition = 1 AND TeamCompetition.TeamCompetitionID IS NULL) OR (@ReturnTeamsNotInCompetition = 0 AND TeamCompetition.TeamCompetitionID IS NOT NULL))
	ORDER BY
		t.TeamName
END
