-- =============================================
-- Author:		David Huggett
-- Create date: 09/03/2012
-- Description:	Returns a list of teams, with their average score between given dates, for a competition, etc
-- =============================================
CREATE PROCEDURE [dbo].[AverageScoreByTeamListGet]
	@CompetitionID UNIQUEIDENTIFIER = NULL
	, @DateFrom DATE = NULL
	, @DateTo DATE = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT
		Team.TeamID
		, Team.ShortName
		, Team.TeamName
		, TeamImage = Team.ImageName
		, AverageScore = AVG(CAST(Prediction.Score AS DECIMAL(9,2)))
	FROM
		Team
		LEFT JOIN (SELECT TeamID FROM TeamCompetition WHERE CompetitionID = @CompetitionID) TeamCompetition ON Team.TeamID = TeamCompetition.TeamID
		INNER JOIN Match ON Match.HomeTeamID = Team.TeamID OR Match.AwayTeamID = Team.TeamID
		INNER JOIN Prediction ON Match.MatchID = Prediction.MatchID
	WHERE
		Prediction.Score IS NOT NULL
		AND (@CompetitionID IS NULL OR TeamCompetition.TeamID IS NOT NULL)
		AND (@DateFrom IS NULL OR CAST(Match.MatchDateTime AS DATE) >= @DateFrom)
		AND (@DateTo IS NULL OR CAST(Match.MatchDateTime AS DATE) <= @DateTo)
	GROUP BY
		Team.TeamID
		, Team.ShortName
		, Team.TeamName
		, Team.ImageName
	ORDER BY
		AverageScore DESC
END
