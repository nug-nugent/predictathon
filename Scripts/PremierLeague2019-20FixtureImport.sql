BEGIN TRAN
	DECLARE @CompetitionID UNIQUEIDENTIFIER = (SELECT CompetitionID FROM Competition WHERE CompetitionName = 'Premier League 2019/20');

	IF @CompetitionID IS NULL
	BEGIN
		PRINT '@CompetitionID IS NULL';
		
		RETURN;
	END;

	DELETE Match WHERE CompetitionID = @CompetitionID
	DELETE TeamCompetition WHERE CompetitionID = @CompetitionID
	
	SET DATEFIRST 1;

	DECLARE @tblTeam TABLE (TeamID UNIQUEIDENTIFIER);
	INSERT @tblTeam SELECT DISTINCT TeamID FROM Team INNER JOIN Import_Match ON Import_Match.HomeTeam = Team.ShortName;

	--SELECT * FROM @tblTeam

	SELECT DISTINCT HomeTeam FROM Import_Match WHERE HomeTeam NOT IN (SELECT TeamName FROM Team);
	SELECT DISTINCT TeamID, TeamName FROM Team INNER JOIN Import_Match ON Import_Match.HomeTeam = Team.TeamName

	INSERT TeamCompetition (TeamCompetitionID, CompetitionID, TeamID) SELECT NEWID(), @CompetitionID, TeamID FROM @tblTeam

	INSERT
		Match (MatchID, CompetitionID, MatchDateTime, HomeTeamID, AwayTeamID, MatchPlayed, HomeTeamGoals, AwayTeamGoals, NeutralGround, HomeTeamTBC, AwayTeamTBC, Description, Knockout)
	SELECT
		MatchID = NEWID()
		, CompetitionID = @CompetitionID
		, CAST(Import_Match.MatchDate AS DATETIME) + Import_Match.MatchTime
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
		Import_Match
		INNER JOIN Team HomeTeam ON Import_Match.HomeTeam = HomeTeam.TeamName
		INNER JOIN Team AwayTeam ON Import_Match.AwayTeam = AwayTeam.TeamName

	--UPDATE Match SET MatchDateTime = DATEADD(HOUR, 15, MatchDateTime) WHERE Match.CompetitionID = @CompetitionID AND DATEPART(WEEKDAY, MatchDateTime) = 6
	--UPDATE Match SET MatchDateTime = DATEADD(HOUR, 16, MatchDateTime) WHERE Match.CompetitionID = @CompetitionID AND DATEPART(WEEKDAY, MatchDateTime) = 7
	--UPDATE Match SET MatchDateTime = DATEADD(HOUR, 20, MatchDateTime) WHERE Match.CompetitionID = @CompetitionID AND DATEPART(WEEKDAY, MatchDateTime) <= 5

	SELECT * FROM Match WHERE CompetitionID = @CompetitionID ORDER BY MatchDateTime
ROLLBACK