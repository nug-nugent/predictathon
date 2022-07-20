-- =============================================
-- Author:		David Huggett
-- Create date: 08/10/2011
-- Description:	Returns the real league table for a given competition
-- =============================================

CREATE PROCEDURE [dbo].[CompetitionRealLeagueTableGet]
	@CompetitionID UNIQUEIDENTIFIER
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT
		Team.TeamID
		, Position =  RANK() OVER (ORDER BY (SUM(Results.Won) * 3) + SUM(Results.Drawn) DESC, SUM(Results.GoalsFor - Results.GoalsAgainst) DESC, SUM(Results.GoalsFor) DESC)
		, Played = COUNT(Results.MatchID)
		, Won = SUM(Results.Won)
		, Lost = SUM(Results.Lost)
		, Drawn = SUM(Results.Drawn)
		, Team.ShortName
		, GoalsFor = SUM(Results.GoalsFor)
		, GoalsAgainst = SUM(Results.GoalsAgainst)
		, Points = (SUM(Results.Won) * 3) + SUM(Results.Drawn)
		, GoalDifference = SUM(Results.GoalsFor - Results.GoalsAgainst)
	FROM
		--Home matches
		(SELECT
			Team.TeamID
			, Match.MatchID
			, Won = CASE WHEN Match.HomeTeamGoals > Match.AwayTeamGoals THEN 1 ELSE 0 END
			, Lost = CASE WHEN Match.HomeTeamGoals < Match.AwayTeamGoals THEN 1 ELSE 0 END
			, Drawn = CASE WHEN Match.HomeTeamGoals = Match.AwayTeamGoals THEN 1 ELSE 0 END
			, GoalsFor = Match.HomeTeamGoals
			, GoalsAgainst = Match.AwayTeamGoals
		FROM
			Match
			INNER JOIN Team ON Match.HomeTeamID = Team.TeamID
		WHERE
			Match.CompetitionID = @CompetitionID
			AND Match.MatchPlayed = 1
		--Away matches
		UNION
		SELECT
			Team.TeamID
			, Match.MatchID
			, Won = CASE WHEN Match.AwayTeamGoals > Match.HomeTeamGoals THEN 1 ELSE 0 END
			, Lost = CASE WHEN Match.AwayTeamGoals < Match.HomeTeamGoals THEN 1 ELSE 0 END
			, Drawn = CASE WHEN Match.AwayTeamGoals = Match.HomeTeamGoals THEN 1 ELSE 0 END
			, GoalsFor = Match.AwayTeamGoals
			, GoalsAgainst = Match.HomeTeamGoals
		FROM
			Match
			INNER JOIN Team ON Match.AwayTeamID = Team.TeamID
		WHERE
			Match.CompetitionID = @CompetitionID
			AND Match.MatchPlayed = 1) Results
		INNER JOIN Team ON Results.TeamID = Team.TeamID
	GROUP BY
		Team.TeamID
		, Team.ShortName
	ORDER BY
		Position
		, Team.ShortName
END
