/* ==========================================================================================
	Description
		Returns a list of teams, with their average score between given dates, for a competition, etc
	
	History
		09/03/2012 : DH - Created
		15/06/2026 : DH - Corrected @CompetitionID logic
========================================================================================== */
CREATE PROCEDURE [dbo].[AverageScoreByTeamListGet]
	@CompetitionID UNIQUEIDENTIFIER = NULL
	, @DateFrom DATE = NULL
	, @DateTo DATE = NULL
AS
BEGIN
	SET NOCOUNT ON;
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

	SELECT
		t.TeamID
		, t.ShortName
		, t.Acronym
		, t.TeamName
		, TeamImage = t.ImageName
		, AverageScore = AVG(CAST(p.Score AS DECIMAL(9,2)))
	FROM
		[dbo].[Team] AS t
		LEFT JOIN (SELECT tc.TeamID FROM [dbo].[TeamCompetition] AS tc WHERE tc.CompetitionID = @CompetitionID) TeamCompetition ON t.TeamID = TeamCompetition.TeamID
		INNER JOIN [dbo].[Match] AS m ON m.HomeTeamID = t.TeamID OR m.AwayTeamID = t.TeamID
		INNER JOIN [dbo].[Prediction] AS p ON m.MatchID = p.MatchID
	WHERE
		p.Score IS NOT NULL
		AND (@CompetitionID IS NULL OR TeamCompetition.TeamID IS NOT NULL)
		AND (@CompetitionID IS NULL OR m.CompetitionID = @CompetitionID)
		AND (@DateFrom IS NULL OR CAST(m.MatchDateTime AS DATE) >= @DateFrom)
		AND (@DateTo IS NULL OR CAST(m.MatchDateTime AS DATE) <= @DateTo)
	GROUP BY
		t.TeamID
		, t.ShortName
		, t.Acronym
		, t.TeamName
		, t.ImageName
	ORDER BY
		AverageScore DESC;
END;
