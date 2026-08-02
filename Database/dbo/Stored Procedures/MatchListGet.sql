-- =============================================
-- Author:		David Huggett
-- Create date: 22/9/11
-- Description:	Returns a list of matches by date
-- =============================================
CREATE PROCEDURE [dbo].[MatchListGet]
	@CompetitionID UNIQUEIDENTIFIER
	, @DateFrom DATETIME = NULL
	, @DateTo DATETIME = NULL
	, @UnprocessedOnly BIT
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		m.MatchID
		, m.MatchDateTime
		, HomeTeam = ISNULL(HomeTeam.TeamName, m.HomeTeamTBC)
		, HomeTeamID = HomeTeam.TeamID
		, HomeTeamImage = HomeTeam.ImageName
		, AwayTeam = ISNULL(AwayTeam.TeamName, m.AwayTeamTBC)
		, AwayTeamID = AwayTeam.TeamID
		, AwayTeamImage = AwayTeam.ImageName
		, m.MatchPlayed
		, m.HomeTeamGoals
		, m.AwayTeamGoals
		, m.NeutralGround
		, m.Description
		, m.HomeTeamTBC
		, m.AwayTeamTBC
		, m.Knockout
	FROM
		[dbo].[Match] AS m
		LEFT JOIN [dbo].[Team] AS HomeTeam ON m.HomeTeamID = HomeTeam.TeamID
		LEFT JOIN [dbo].[Team] AS AwayTeam ON m.AwayTeamID = AwayTeam.TeamID
	WHERE
		m.CompetitionID = @CompetitionID
		AND (@DateFrom IS NULL OR m.MatchDateTime >= @DateFrom)
		AND (@DateTo IS NULL OR m.MatchDateTime <= @DateTo)
		AND (@UnprocessedOnly = 0 OR m.MatchPlayed = 0)
	ORDER BY
		m.MatchDateTime ASC
		, HomeTeam.TeamName;
END;