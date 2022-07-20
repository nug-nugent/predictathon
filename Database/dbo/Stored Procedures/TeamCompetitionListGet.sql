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
		, Team.TeamID
		, Team.TeamName
	FROM
		Team
		LEFT JOIN (SELECT * FROM TeamCompetition WHERE CompetitionID = @CompetitionID) TeamCompetition ON TeamCompetition.TeamID = Team.TeamID
	WHERE
		((@ReturnTeamsNotInCompetition = 1 AND TeamCompetition.TeamCompetitionID IS NULL) OR (@ReturnTeamsNotInCompetition = 0 AND TeamCompetition.TeamCompetitionID IS NOT NULL))
	ORDER BY
		Team.TeamName
END
