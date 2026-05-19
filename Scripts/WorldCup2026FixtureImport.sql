USE [predicta_predictathon];

BEGIN TRAN
	DECLARE @CompetitionID UNIQUEIDENTIFIER = '05B5FDD0-C358-4845-BB7B-9724D2C9CED2';
	
	IF NOT EXISTS(SELECT 1 FROM Competition WHERE CompetitionID = @CompetitionID)
	BEGIN
		INSERT
			Competition (CompetitionID, CompetitionName, PrependNameWithThe, StartDate, EndDate, DuplicateFixturesAllowed, OpenForRegistration, RegistrationAvailableOnLoginPage, ShowInHallOfFame, EntranceFee, PayPalPaymentAvailable, Information, ImageFilename, DefaultToNeutralGround)
		SELECT
			@CompetitionID
			, 'World Cup 2026'
			, PrependNameWithThe
			, (SELECT MIN(MatchDate) FROM Import_Match)
			, (SELECT MAX(MatchDate) FROM Import_Match)
			, DuplicateFixturesAllowed
			, OpenForRegistration
			, RegistrationAvailableOnLoginPage
			, ShowInHallOfFame
			, EntranceFee
			, PayPalPaymentAvailable
			, 'The World Cup - where it all started for Predictathon, back in 1998. With more matches than usual, this is still short, sharp, competitive, and always the most popular Predictathon tournament going.'
			, 'WorldCup2026.png' -- ImageFilename
			, DefaultToNeutralGround	
		FROM 
			Competition 
		WHERE 
			CompetitionName = 'World Cup 2022';
	END;

	DELETE Match WHERE CompetitionID = @CompetitionID;
	DELETE TeamCompetition WHERE CompetitionID = @CompetitionID;
	
	DECLARE @tblTeam TABLE (TeamID UNIQUEIDENTIFIER);
	INSERT @tblTeam SELECT DISTINCT TeamID FROM Team INNER JOIN Import_Match ON Import_Match.HomeTeam = Team.ShortName;

	SELECT DISTINCT HomeTeam FROM Import_Match WHERE Knockout = 0 AND HomeTeam NOT IN (SELECT ShortName FROM Team);
	
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
	--		TeamName = HomeTeam
	--	FROM
	--		Import_Match
	--	WHERE
	--		Knockout = 0
	--		AND HomeTeam NOT IN (SELECT ShortName FROM Team)
	--		AND NULLIF(HomeTeam, '') IS NOT NULL
	--	UNION
	--	SELECT
	--		TeamName = AwayTeam
	--	FROM
	--		Import_Match
	--	WHERE
	--		Knockout = 0
	--		AND AwayTeam NOT IN (SELECT ShortName FROM Team)
	--		AND NULLIF(AwayTeam, '') IS NOT NULL) NewTeams;

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
	--		TeamName = HomeTeam
	--	FROM
	--		Import_Match
	--	WHERE
	--		HomeTeam NOT IN (SELECT ShortName FROM Team)
	--		AND NULLIF(HomeTeam, '') IS NOT NULL
	--		AND Knockout = 0
	--	UNION
	--	SELECT
	--		TeamName = AwayTeam
	--	FROM
	--		Import_Match
	--	WHERE
	--		AwayTeam NOT IN (SELECT ShortName FROM Team)
	--		AND Knockout = 0
	--		AND NULLIF(AwayTeam, '') IS NOT NULL) NewTeams;

	INSERT
		TeamCompetition (TeamCompetitionID, TeamID, CompetitionID)
	SELECT
		NEWID()
		, TeamID
		, @CompetitionID
	FROM
		Team
		INNER JOIN (SELECT DISTINCT HomeTeam FROM Import_Match) Import_Match ON Team.ShortName = Import_Match.HomeTeam;

	INSERT
		Match (MatchID, CompetitionID, MatchDateTime, HomeTeamID, AwayTeamID, MatchPlayed, NeutralGround, HomeTeamTBC, AwayTeamTBC, Description, Knockout)
	SELECT
		MatchID = NEWID()
		, CompetitionID = @CompetitionID
		, MatchDateTime = CAST(CAST(Import_Match.MatchDate AS DATE) AS DATETIME) + CAST(Import_Match.MatchTime AS TIME)
		, HomeTeamTeamID = HomeTeam.TeamID
		, AwayTeamTeamID = AwayTeam.TeamID
		, MatchPlayed = 0
		, NeutralGround = 1
		, HomeTeamTeamTBC = CASE WHEN HomeTeam.TeamID IS NULL THEN Import_Match.HomeTeam ELSE NULL END
		, AwayTeamTeamTBC = CASE WHEN HomeTeam.TeamID IS NULL THEN Import_Match.AwayTeam ELSE NULL END
		, Description = Import_Match.Description
		, Knockout = Import_Match.Knockout
	FROM
		Import_Match
		LEFT JOIN Team HomeTeam ON Import_Match.HomeTeam = HomeTeam.ShortName
		LEFT JOIN Team AwayTeam ON Import_Match.AwayTeam = AwayTeam.ShortName;

	SELECT
		HomeTeam = HomeTeam.ShortName
		, AwayTeam = AwayTeam.ShortName
		, Match.Description
		, Match.MatchDateTime
		, Match.HomeTeamTBC
		, Match.AwayTeamTBC
	FROM
		Match
		LEFT JOIN Team HomeTeam ON Match.HomeTeamID = HomeTeam.TeamID
		LEFT JOIN Team AwayTeam ON Match.AwayTeamID = AwayTeam.TeamID
	WHERE
		CompetitionID = @CompetitionID
	ORDER BY
		MatchDateTime;
ROLLBACK;