USE [predicta_predictathon];

BEGIN TRAN
	DECLARE @CompetitionID UNIQUEIDENTIFIER = 'FE6439ED-45C3-459B-97B7-649BAAC918CF';
	
	--DELETE TeamCompetition WHERE CompetitionID = @CompetitionID;
	--DELETE Match WHERE CompetitionID = @CompetitionID;
	--DELETE Competition WHERE CompetitionID = @CompetitionID;

	--INSERT
	--	Competition (CompetitionID, CompetitionName, PrependNameWithThe, StartDate, EndDate, DuplicateFixturesAllowed, OpenForRegistration, RegistrationAvailableOnLoginPage, ShowInHallOfFame, EntranceFee, PayPalPaymentAvailable, Information, ImageFilename, DefaultToNeutralGround)
	--SELECT
	--	@CompetitionID
	--	, 'World Cup 2018'
	--	, PrependNameWithThe
	--	, (SELECT MIN(MatchDate) FROM Import_Match)
	--	, (SELECT MAX(MatchDate) FROM Import_Match)
	--	, DuplicateFixturesAllowed
	--	, OpenForRegistration
	--	, RegistrationAvailableOnLoginPage
	--	, ShowInHallOfFame
	--	, EntranceFee
	--	, PayPalPaymentAvailable
	--	, 'The World Cup - where it all started for Predictathon, back in 1998. With 64 matches in total, this is short, sharp, competitive, and always the most popular Predictathon tournament going.'
	--	, ImageFilename
	--	, DefaultToNeutralGround	
	--FROM 
	--	Competition 
	--WHERE 
	--	CompetitionName = 'World Cup 2014';

	PRINT 'Creating the following new teams:'
	SELECT
		TeamName
		, 'International/Europe/' + TeamName + '.gif'
	FROM
		(SELECT
			TeamName = Home
		FROM
			Import_Match
		WHERE
			Home NOT IN (SELECT ShortName FROM Team)
			AND NULLIF(Home, '') IS NOT NULL
		UNION
		SELECT
			TeamName = Away
		FROM
			Import_Match
		WHERE
			Away NOT IN (SELECT ShortName FROM Team)
			AND NULLIF(Away, '') IS NOT NULL) NewTeams
	WHERE
		NewTeams.TeamName NOT LIKE 'Group%'
		AND NewTeams.TeamName NOT LIKE 'Winner%'
		AND NewTeams.TeamName NOT LIKE 'Loser%';

	--Teams
	INSERT
		Team (TeamID, TeamName, ShortName, ImageName)
	SELECT
		NEWID()
		, TeamName
		, TeamName
		, 'International/Europe/' + TeamName + '.gif'
	FROM
		(SELECT
			TeamName = Home
		FROM
			Import_Match
		WHERE
			Home NOT IN (SELECT ShortName FROM Team)
			AND NULLIF(Home, '') IS NOT NULL
		UNION
		SELECT
			TeamName = Away
		FROM
			Import_Match
		WHERE
			Away NOT IN (SELECT ShortName FROM Team)
			AND NULLIF(Away, '') IS NOT NULL) NewTeams
	WHERE
		NewTeams.TeamName NOT LIKE 'Group%'
		AND NewTeams.TeamName NOT LIKE 'Winner%'
		AND NewTeams.TeamName NOT LIKE 'Loser%';

	INSERT
		TeamCompetition (TeamCompetitionID, TeamID, CompetitionID)
	SELECT
		NEWID()
		, TeamID
		, @CompetitionID
	FROM
		Team
		INNER JOIN (SELECT DISTINCT Home FROM Import_Match) Import_Match ON Team.TeamName = Import_Match.Home;

	INSERT
		Match (MatchID, CompetitionID, MatchDateTime, HomeTeamID, AwayTeamID, MatchPlayed, NeutralGround, HomeTeamTBC, AwayTeamTBC, Description, Knockout)
	SELECT
		MatchID = NEWID()
		, CompetitionID = @CompetitionID
		, MatchDateTime = CAST(CAST(Import_Match.[Date] AS DATE) AS DATETIME) + CAST(Import_Match.[Time] AS TIME)
		, HomeTeamID = Home.TeamID
		, AwayTeamID = Away.TeamID
		, MatchPlayed = 0
		, NeutralGround = 1
		, HomeTeamTBC = CASE WHEN Home.TeamID IS NULL THEN Import_Match.Home ELSE NULL END
		, AwayTeamTBC = CASE WHEN Home.TeamID IS NULL THEN Import_Match.Away ELSE NULL END
		, Description = Import_Match.Description
		, Knockout = CASE WHEN NULLIF(Import_Match.Home, '') IS NULL THEN 1 ELSE 0 END
	FROM
		Import_Match
		LEFT JOIN Team Home ON Import_Match.Home = Home.TeamName
		LEFT JOIN Team Away ON Import_Match.Away = Away.TeamName;

	SELECT
		Home = Home.ShortName
		, Away = Away.ShortName
		, Match.Description
		, Match.MatchDateTime
		, Match.HomeTeamTBC
		, Match.AwayTeamTBC
	FROM
		Match
		LEFT JOIN Team Home ON Match.HomeTeamID = Home.TeamID
		LEFT JOIN Team Away ON Match.AwayTeamID = Away.TeamID
	WHERE
		CompetitionID = @CompetitionID
	ORDER BY
		MatchDateTime;
ROLLBACK;