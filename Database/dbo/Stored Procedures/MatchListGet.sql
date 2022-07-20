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
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT
		Match.MatchID
		, Match.MatchDateTime
		, HomeTeam = ISNULL(HomeTeam.TeamName, Match.HomeTeamTBC)
		, HomeTeamID = HomeTeam.TeamID
		, HomeTeamImage = HomeTeam.ImageName
		, AwayTeam = ISNULL(AwayTeam.TeamName, Match.AwayTeamTBC)
		, AwayTeamID = AwayTeam.TeamID
		, AwayTeamImage = AwayTeam.ImageName
		, Match.MatchPlayed
		, Match.HomeTeamGoals
		, Match.AwayTeamGoals
		, Match.NeutralGround
		, Match.Description
		, Match.HomeTeamTBC
		, Match.AwayTeamTBC
		, Match.Knockout
	FROM
		Match
		LEFT JOIN Team HomeTeam ON Match.HomeTeamID = HomeTeam.TeamID
		LEFT JOIN Team AwayTeam ON Match.AwayTeamID = AwayTeam.TeamID
	WHERE
		Match.CompetitionID = @CompetitionID
		AND (@DateFrom IS NULL OR Match.MatchDateTime >= @DateFrom) 
		AND (@DateTo IS NULL OR Match.MatchDateTime <= @DateTo)
		AND (@UnprocessedOnly = 0 OR Match.MatchPlayed = 0)
	ORDER BY
		Match.MatchDateTime ASC
		, HomeTeam.TeamName
END
