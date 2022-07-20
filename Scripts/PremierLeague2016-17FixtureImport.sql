BEGIN TRAN
	DELETE Team WHERE TeamName = 'Middlesbrough'
	INSERT Team (TeamID, TeamName, ShortName, ImageName)
	SELECT NEWID(), 'Middlesbrough', 'Middlesbrough', 'Middlesbrough.gif'

	DECLARE @CompetitionID UNIQUEIDENTIFIER = (SELECT CompetitionID FROM Competition WHERE CompetitionName = 'Premier League 2016/17')
	DELETE Match WHERE CompetitionID = @CompetitionID
	DELETE TeamCompetition WHERE CompetitionID = @CompetitionID

	SET DATEFIRST 1;

	DECLARE @tblTeam TABLE (TeamID UNIQUEIDENTIFIER) 
	INSERT @tblTeam SELECT DISTINCT TeamID FROM Team INNER JOIN TEMP_IMPORT_PremierLeague ON TEMP_IMPORT_PremierLeague.HomeTeam = Team.ShortName

	--SELECT DISTINCT TeamID, TeamName FROM Team INNER JOIN TEMP_IMPORT_PremierLeague ON TEMP_IMPORT_PremierLeague.HomeTeam = Team.TeamName

	INSERT TeamCompetition (TeamCompetitionID, CompetitionID, TeamID) SELECT NEWID(), @CompetitionID, TeamID FROM @tblTeam

	INSERT
		Match (MatchID, CompetitionID, MatchDateTime, HomeTeamID, AwayTeamID, MatchPlayed, HomeTeamGoals, AwayTeamGoals, NeutralGround, HomeTeamTBC, AwayTeamTBC, Description, Knockout)
	SELECT
		MatchID = NEWID()
		, CompetitionID = @CompetitionID
		, TEMP_IMPORT_PremierLeague.MatchDate + TEMP_Import_PremierLeague.MatchTime
		, HomeTeamID = HomeTeam.TeamID
		, AwayTeamID = AwayTeam.TeamID
		, MatchPlayed = 0
		, HomeTeamGoals = NULL
		, AwayTeamGoals = NULL
		, NeutralGround = 0
		, HomeTeamTBC = NULL
		, AwayTeamTBC = NULL
		, Description = NULL
		, Knockout = 0
	FROM
		TEMP_IMPORT_PremierLeague
		INNER JOIN Team HomeTeam ON TEMP_IMPORT_PremierLeague.HomeTeam = HomeTeam.ShortName
		INNER JOIN Team AwayTeam ON TEMP_IMPORT_PremierLeague.AwayTeam = AwayTeam.ShortName

	--UPDATE Match SET MatchDateTime = DATEADD(HOUR, 15, MatchDateTime) WHERE Match.CompetitionID = @CompetitionID AND DATEPART(WEEKDAY, MatchDateTime) = 6
	--UPDATE Match SET MatchDateTime = DATEADD(HOUR, 16, MatchDateTime) WHERE Match.CompetitionID = @CompetitionID AND DATEPART(WEEKDAY, MatchDateTime) = 7
	--UPDATE Match SET MatchDateTime = DATEADD(HOUR, 20, MatchDateTime) WHERE Match.CompetitionID = @CompetitionID AND DATEPART(WEEKDAY, MatchDateTime) <= 5

	SELECT * FROM Match WHERE CompetitionID = @CompetitionID ORDER BY MatchDateTime
ROLLBACK