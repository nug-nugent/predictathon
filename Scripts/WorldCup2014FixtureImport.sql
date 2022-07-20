BEGIN TRAN
	DECLARE @CompetitionID UNIQUEIDENTIFIER = 'CCF70A73-69BE-4D2A-BD50-1A85F0EC1EE4'

	INSERT
		Competition (CompetitionID, CompetitionName, PrependNameWithThe, StartDate, EndDate, DuplicateFixturesAllowed, OpenForRegistration, RegistrationAvailableOnLoginPage, ShowInHallOfFame, EntranceFee, PayPalPaymentAvailable, Information, ImageFilename, DefaultToNeutralGround)
	SELECT
		@CompetitionID
		, 'World Cup 2014'
		, PrependNameWithThe
		, '20140612'
		, '20140713'
		, DuplicateFixturesAllowed
		, OpenForRegistration
		, RegistrationAvailableOnLoginPage
		, ShowInHallOfFame
		, EntranceFee
		, PayPalPaymentAvailable
		, 'The World Cup - where it all started for Predictathon, back in 1998.'
		, ImageFilename
		, DefaultToNeutralGround	
	FROM 
		Competition 
	WHERE 
		CompetitionName = 'Euro 2012'

	--Teams
	INSERT
		Team (TeamID, TeamName, ShortName, ImageName)
	SELECT
		NEWID()
		, TeamName
		, TeamName
		, 'International/' + TeamName + '.gif'
	FROM
		(SELECT
			TeamName = Home
		FROM
			TEMP_IMPORT_WorldCup
		WHERE
			Home NOT IN (SELECT ShortName FROM Team)
			AND MatchDate < '20140628'
		UNION
		SELECT
			TeamName = Away
		FROM
			TEMP_IMPORT_WorldCup
		WHERE
			Away NOT IN (SELECT ShortName FROM Team)
			AND MatchDate < '20140628') NewTeams

	INSERT
		TeamCompetition (TeamCompetitionID, TeamID, CompetitionID)
	SELECT
		NEWID()
		, TeamID
		, @CompetitionID
	FROM
		Team
		INNER JOIN (SELECT DISTINCT Home FROM TEMP_IMPORT_WorldCup) TEMP_IMPORT_WorldCup ON Team.TeamName = TEMP_IMPORT_WorldCup.Home

	UPDATE Team SET ShortName = 'Bosnia', ImageName = 'International/Bosnia.gif' WHERE ShortName = 'Bosnia-Herzegovina'
	UPDATE Team SET ImageName = 'International/SouthKorea.gif' WHERE ShortName = 'South Korea'
	UPDATE Team SET ImageName = 'International/CostaRica.gif' WHERE ShortName = 'Costa Rica'
	UPDATE Team SET ImageName = 'International/IvoryCoast.gif' WHERE ShortName = 'Ivory Coast'

	UPDATE 
		Team 
	SET 
		ImageName = REPLACE(ImageName, 'International', 'International/Europe')
	FROM
		Team
		INNER JOIN TeamCompetition ON Team.TeamID = TeamCompetition.TeamID
	WHERE 
		ShortName IN ('Belgium', 'Bosnia', 'Switzerland')
		AND CompetitionID = @CompetitionID
		AND ImageName NOT LIKE 'International/Europe/%'

	INSERT
		Match (MatchID, CompetitionID, MatchDateTime, HomeTeamID, AwayTeamID, MatchPlayed, HomeTeamGoals, AwayTeamGoals, NeutralGround, HomeTeamTBC, AwayTeamTBC, Description, Knockout)
	SELECT
		MatchID = NEWID()
		, CompetitionID = @CompetitionID
		, MatchDateTime = CAST(TEMP_IMPORT_WorldCup.MatchDate AS DATETIME) + TEMP_IMPORT_WorldCup.MatchTime
		, HomeTeamID = HomeTeam.TeamID
		, AwayTeamID = AwayTeam.TeamID
		, MatchPlayed = 0
		, HomeTeamGoals = NULL
		, AwayTeamGoals = NULL
		, NeutralGround = 1
		, HomeTeamTBC = CASE WHEN HomeTeam.TeamID IS NULL THEN TEMP_IMPORT_WorldCup.Home ELSE NULL END
		, AwayTeamTBC = CASE WHEN HomeTeam.TeamID IS NULL THEN TEMP_IMPORT_WorldCup.Away ELSE NULL END
		, Description = TEMP_IMPORT_WorldCup.Description
		, Knockout = CASE WHEN MatchDate < '20140628' THEN 0 ELSE 1 END
	FROM
		TEMP_IMPORT_WorldCup
		LEFT JOIN Team HomeTeam ON TEMP_IMPORT_WorldCup.Home = HomeTeam.TeamName
		LEFT JOIN Team AwayTeam ON TEMP_IMPORT_WorldCup.Away = AwayTeam.TeamName
ROLLBACK