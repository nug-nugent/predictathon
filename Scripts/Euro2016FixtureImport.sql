BEGIN TRAN
	DECLARE @CompetitionID UNIQUEIDENTIFIER = (SELECT CompetitionID FROM Competition WHERE CompetitionName = 'Euro 2016')
	
	INSERT
		Match (MatchID, CompetitionID, MatchDateTime, HomeTeamID, AwayTeamID, MatchPlayed, HomeTeamGoals, AwayTeamGoals, NeutralGround, HomeTeamTBC, AwayTeamTBC, Description, Knockout)
	SELECT
		MatchID = NEWID()
		, CompetitionID = @CompetitionID
		, MatchDateTime = CAST(IMPORT_Euro2016.Date AS DATETIME) + CAST(IMPORT_Euro2016.Time AS TIME)
		, HomeTeamID = HomeTeam.TeamID
		, AwayTeamID = AwayTeam.TeamID
		, MatchPlayed = 0
		, HomeTeamGoals = NULL
		, AwayTeamGoals = NULL
		, NeutralGround = 1
		, HomeTeamTBC = CASE WHEN HomeTeam.TeamID IS NULL THEN IMPORT_Euro2016.Home ELSE NULL END
		, AwayTeamTBC = CASE WHEN HomeTeam.TeamID IS NULL THEN IMPORT_Euro2016.Away ELSE NULL END
		, Description = IMPORT_Euro2016.Description
		, Knockout = CASE WHEN IMPORT_Euro2016.Date < '20160625' THEN 0 ELSE 1 END
	FROM
		IMPORT_Euro2016
		LEFT JOIN Team HomeTeam ON IMPORT_Euro2016.Home = HomeTeam.TeamName
		LEFT JOIN Team AwayTeam ON IMPORT_Euro2016.Away = AwayTeam.TeamName
ROLLBACK

--SELECT * FROM IMPORT_Euro2016 ORDER BY Date