-- =============================================
-- Author:		David Huggett
-- Create date: 08/10/2011
-- Description:	Returns the real league table for a given competition
-- =============================================

CREATE PROCEDURE [dbo].[CompetitionRealLeagueTableGet]
	@CompetitionID UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		t.TeamID
		, Position =  RANK() OVER (ORDER BY (SUM(Results.Won) * 3) + SUM(Results.Drawn) DESC, SUM(Results.GoalsFor - Results.GoalsAgainst) DESC, SUM(Results.GoalsFor) DESC)
		, Played = COUNT(Results.MatchID)
		, Won = SUM(Results.Won)
		, Lost = SUM(Results.Lost)
		, Drawn = SUM(Results.Drawn)
		, t.ShortName
		, GoalsFor = SUM(Results.GoalsFor)
		, GoalsAgainst = SUM(Results.GoalsAgainst)
		, Points = (SUM(Results.Won) * 3) + SUM(Results.Drawn)
		, GoalDifference = SUM(Results.GoalsFor - Results.GoalsAgainst)
	FROM
		--Home matches
		(SELECT
			t.TeamID
			, m.MatchID
			, Won = CASE WHEN m.HomeTeamGoals > m.AwayTeamGoals THEN 1 ELSE 0 END
			, Lost = CASE WHEN m.HomeTeamGoals < m.AwayTeamGoals THEN 1 ELSE 0 END
			, Drawn = CASE WHEN m.HomeTeamGoals = m.AwayTeamGoals THEN 1 ELSE 0 END
			, GoalsFor = m.HomeTeamGoals
			, GoalsAgainst = m.AwayTeamGoals
		FROM
			[dbo].[Match] AS m
			INNER JOIN [dbo].[Team] AS t ON m.HomeTeamID = t.TeamID
		WHERE
			m.CompetitionID = @CompetitionID
			AND m.MatchPlayed = 1
		--Away matches
		UNION
		SELECT
			t.TeamID
			, m.MatchID
			, Won = CASE WHEN m.AwayTeamGoals > m.HomeTeamGoals THEN 1 ELSE 0 END
			, Lost = CASE WHEN m.AwayTeamGoals < m.HomeTeamGoals THEN 1 ELSE 0 END
			, Drawn = CASE WHEN m.AwayTeamGoals = m.HomeTeamGoals THEN 1 ELSE 0 END
			, GoalsFor = m.AwayTeamGoals
			, GoalsAgainst = m.HomeTeamGoals
		FROM
			[dbo].[Match] AS m
			INNER JOIN [dbo].[Team] AS t ON m.AwayTeamID = t.TeamID
		WHERE
			m.CompetitionID = @CompetitionID
			AND m.MatchPlayed = 1) Results
		INNER JOIN [dbo].[Team] AS t ON Results.TeamID = t.TeamID
	GROUP BY
		t.TeamID
		, t.ShortName
	ORDER BY
		Position
		, t.ShortName;
END;