USE [predicta_predictathon];

BEGIN TRAN
	DECLARE @CompetitionID UNIQUEIDENTIFIER = '6EFD6AB1-3EAF-4039-B9F1-004D689C5CEA';

	IF NOT EXISTS(SELECT 1 FROM Competition WHERE CompetitionID = @CompetitionID)
	BEGIN
		INSERT
			Competition (CompetitionID, CompetitionName, PrependNameWithThe, StartDate, EndDate, DuplicateFixturesAllowed, OpenForRegistration, RegistrationAvailableOnLoginPage, ShowInHallOfFame, EntranceFee, PayPalPaymentAvailable, Information, ImageFilename, DefaultToNeutralGround)
		SELECT
			@CompetitionID
			, 'Euro 2024'
			, PrependNameWithThe
			, (SELECT MIN(MatchDate) FROM Import_Match)
			, (SELECT MAX(MatchDate) FROM Import_Match)
			, DuplicateFixturesAllowed
			, OpenForRegistration
			, RegistrationAvailableOnLoginPage
			, ShowInHallOfFame
			, EntranceFee
			, PayPalPaymentAvailable
			, 'The European Championships - the Euros features 24 teams, which makes for a quick-fire international tournament.
<br />It''s anyone''s for the taking - your £10 will go straight into the prize pot, to be distributed between the top 3 players.
<div style="margin-top: 15px;" class="Yes">
New users - sign up here!
</div>'
			, 'EuroTrophy.gif' -- ImageFilename
			, DefaultToNeutralGround	
		FROM 
			Competition 
		WHERE 
			CompetitionName = 'World Cup 2022';
	END;

	DELETE Match WHERE CompetitionID = @CompetitionID;
	DELETE TeamCompetition WHERE CompetitionID = @CompetitionID;
	
	DECLARE @tblTeam TABLE (TeamID UNIQUEIDENTIFIER);
	INSERT @tblTeam SELECT DISTINCT TeamID FROM Team INNER JOIN Import_Match ON Import_Match.HomeTeam = Team.TeamName;

	--SELECT DISTINCT HomeTeam FROM Import_Match WHERE Knockout = 0 AND HomeTeam NOT IN (SELECT TeamName FROM Team);
	--IF @@ROWCOUNT > 0 
	--BEGIN
	--	PRINT 'Some teams don''t exist - script aborted.';
	--	ROLLBACK TRANSACTION;
	--	RETURN;
	--END;

	PRINT 'Creating the following new teams:'
	SELECT
		TeamName
		, 'International/Europe/' + TeamName + '.gif'
	FROM
		(SELECT
			TeamName = HomeTeam
		FROM
			Import_Match
		WHERE
			Knockout = 0
			AND HomeTeam NOT IN (SELECT ShortName FROM Team)
			AND NULLIF(HomeTeam, '') IS NOT NULL
		UNION
		SELECT
			TeamName = AwayTeam
		FROM
			Import_Match
		WHERE
			Knockout = 0
			AND AwayTeam NOT IN (SELECT ShortName FROM Team)
			AND NULLIF(AwayTeam, '') IS NOT NULL) NewTeams;

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
			TeamName = HomeTeam
		FROM
			Import_Match
		WHERE
			HomeTeam NOT IN (SELECT ShortName FROM Team)
			AND NULLIF(HomeTeam, '') IS NOT NULL
			AND Knockout = 0
		UNION
		SELECT
			TeamName = AwayTeam
		FROM
			Import_Match
		WHERE
			AwayTeam NOT IN (SELECT ShortName FROM Team)
			AND Knockout = 0
			AND NULLIF(AwayTeam, '') IS NOT NULL) NewTeams;

	INSERT
		TeamCompetition (TeamCompetitionID, TeamID, CompetitionID)
	SELECT
		NEWID()
		, TeamID
		, @CompetitionID
	FROM
		Team
		INNER JOIN (SELECT DISTINCT HomeTeam FROM Import_Match) Import_Match ON Team.TeamName = Import_Match.HomeTeam;

	INSERT
		Match (MatchID, CompetitionID, MatchDateTime, HomeTeamID, AwayTeamID, MatchPlayed, NeutralGround, HomeTeamTBC, AwayTeamTBC, Description, Knockout)
	SELECT
		MatchID = NEWID()
		, CompetitionID = @CompetitionID
		, MatchDateTime = CAST(CAST(Import_Match.[MatchDate] AS DATE) AS DATETIME) + CAST(Import_Match.MatchTime AS TIME)
		, HomeTeamID = Home.TeamID
		, AwayTeamID = Away.TeamID
		, MatchPlayed = 0
		, NeutralGround = 1
		, HomeTeamTBC = CASE WHEN Home.TeamID IS NULL THEN Import_Match.HomeTeam ELSE NULL END
		, AwayTeamTBC = CASE WHEN Home.TeamID IS NULL THEN Import_Match.AwayTeam ELSE NULL END
		, Description = Import_Match.Description
		, Knockout = Import_Match.Knockout
	FROM
		Import_Match
		LEFT JOIN Team Home ON Import_Match.HomeTeam = Home.TeamName
		LEFT JOIN Team Away ON Import_Match.AwayTeam = Away.TeamName;

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