USE [predicta_predictathon];

BEGIN TRAN
	DECLARE @CompetitionID UNIQUEIDENTIFIER = 'A38508A3-83E6-4D8E-83E1-5972AF270E12';

	IF NOT EXISTS(SELECT 1 FROM Competition WHERE CompetitionID = @CompetitionID)
	BEGIN
		INSERT
			Competition (CompetitionID, CompetitionName, PrependNameWithThe, StartDate, EndDate, DuplicateFixturesAllowed, OpenForRegistration, RegistrationAvailableOnLoginPage, ShowInHallOfFame, EntranceFee, PayPalPaymentAvailable, Information, ImageFilename, DefaultToNeutralGround)
		SELECT
			@CompetitionID
			, 'World Cup 2022'
			, PrependNameWithThe
			, (SELECT MIN(Date) FROM Import_Match)
			, (SELECT MAX(Date) FROM Import_Match)
			, DuplicateFixturesAllowed
			, OpenForRegistration
			, RegistrationAvailableOnLoginPage
			, ShowInHallOfFame
			, EntranceFee
			, PayPalPaymentAvailable
			, 'The World Cup - where it all started for Predictathon, back in 1998. With 64 matches in total, this is short, sharp, competitive, and always the most popular Predictathon tournament going.'
			, 'WorldCup2022.png' -- ImageFilename
			, DefaultToNeutralGround	
		FROM 
			Competition 
		WHERE 
			CompetitionName = 'World Cup 2018';
	END;

	DELETE Match WHERE CompetitionID = @CompetitionID;
	DELETE TeamCompetition WHERE CompetitionID = @CompetitionID;
	
	DECLARE @tblTeam TABLE (TeamID UNIQUEIDENTIFIER);
	INSERT @tblTeam SELECT DISTINCT TeamID FROM Team INNER JOIN Import_Match ON Import_Match.Home = Team.TeamName;

	SELECT DISTINCT Home FROM Import_Match WHERE Knockout = 0 AND Home NOT IN (SELECT TeamName FROM Team);
	
	IF @@ROWCOUNT > 0 
	BEGIN
		PRINT 'Some teams don''t exist - script aborted.';
		ROLLBACK TRANSACTION;
		RETURN;
	END;

	--PRINT 'Creating the following new teams:'
	--SELECT
	--	TeamName
	--	, 'International/Europe/' + TeamName + '.gif'
	--FROM
	--	(SELECT
	--		TeamName = Home
	--	FROM
	--		Import_Match
	--	WHERE
	--		Knockout = 0
	--		AND Home NOT IN (SELECT ShortName FROM Team)
	--		AND NULLIF(Home, '') IS NOT NULL
	--	UNION
	--	SELECT
	--		TeamName = Away
	--	FROM
	--		Import_Match
	--	WHERE
	--		Knockout = 0
	--		AND Away NOT IN (SELECT ShortName FROM Team)
	--		AND NULLIF(Away, '') IS NOT NULL) NewTeams;

	--Teams
	--INSERT
	--	Team (TeamID, TeamName, ShortName, ImageName)
	--SELECT
	--	NEWID()
	--	, TeamName
	--	, TeamName
	--	, 'International/Europe/' + TeamName + '.gif'
	--FROM
	--	(SELECT
	--		TeamName = Home
	--	FROM
	--		Import_Match
	--	WHERE
	--		Home NOT IN (SELECT ShortName FROM Team)
	--		AND NULLIF(Home, '') IS NOT NULL
	--		AND Knockout = 0
	--	UNION
	--	SELECT
	--		TeamName = Away
	--	FROM
	--		Import_Match
	--	WHERE
	--		Away NOT IN (SELECT ShortName FROM Team)
	--		AND Knockout = 0
	--		AND NULLIF(Away, '') IS NOT NULL) NewTeams;

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
		, Knockout = Import_Match.Knockout
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