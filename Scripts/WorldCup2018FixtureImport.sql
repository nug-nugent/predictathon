BEGIN TRAN
	DECLARE @CompetitionID UNIQUEIDENTIFIER = '1271393E-9EDC-4F6B-8F61-D3698E6E68D6'
	--DELETE TeamCompetition WHERE CompetitionID = @CompetitionID;
	--DELETE Match WHERE CompetitionID = @CompetitionID;
	--DELETE Competition WHERE CompetitionID = @CompetitionID;

	INSERT
		Competition (CompetitionID, CompetitionName, PrependNameWithThe, StartDate, EndDate, DuplicateFixturesAllowed, OpenForRegistration, RegistrationAvailableOnLoginPage, ShowInHallOfFame, EntranceFee, PayPalPaymentAvailable, Information, ImageFilename, DefaultToNeutralGround)
	SELECT
		@CompetitionID
		, 'World Cup 2018'
		, PrependNameWithThe
		, (SELECT MIN(MatchDate) FROM Import_Match)
		, (SELECT MAX(MatchDate) FROM Import_Match)
		, DuplicateFixturesAllowed
		, OpenForRegistration
		, RegistrationAvailableOnLoginPage
		, ShowInHallOfFame
		, EntranceFee
		, PayPalPaymentAvailable
		, 'The World Cup - where it all started for Predictathon, back in 1998. With 64 matches in total, this is short, sharp, competitive, and always the most popular Predictathon tournament going.'
		, ImageFilename
		, DefaultToNeutralGround	
	FROM 
		Competition 
	WHERE 
		CompetitionName = 'World Cup 2014';

	PRINT 'Creating the following new teams:'
	SELECT
		TeamName
		, 'International/' + TeamName + '.gif'
	FROM
		(SELECT
			TeamName = HomeTeam
		FROM
			Import_Match
		WHERE
			HomeTeam NOT IN (SELECT ShortName FROM Team)
			AND NULLIF(HomeTeam, '') IS NOT NULL
		UNION
		SELECT
			TeamName = AwayTeam
		FROM
			Import_Match
		WHERE
			AwayTeam NOT IN (SELECT ShortName FROM Team)
			AND NULLIF(AwayTeam, '') IS NOT NULL) NewTeams
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
		, 'International/' + TeamName + '.gif'
	FROM
		(SELECT
			TeamName = HomeTeam
		FROM
			Import_Match
		WHERE
			HomeTeam NOT IN (SELECT ShortName FROM Team)
			AND NULLIF(HomeTeam, '') IS NOT NULL
		UNION
		SELECT
			TeamName = AwayTeam
		FROM
			Import_Match
		WHERE
			AwayTeam NOT IN (SELECT ShortName FROM Team)
			AND NULLIF(AwayTeam, '') IS NOT NULL) NewTeams
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
		INNER JOIN (SELECT DISTINCT HomeTeam FROM Import_Match) Import_Match ON Team.TeamName = Import_Match.HomeTeam;

	INSERT
		Match (MatchID, CompetitionID, MatchDateTime, HomeTeamID, AwayTeamID, MatchPlayed, HomeTeamGoals, AwayTeamGoals, NeutralGround, HomeTeamTBC, AwayTeamTBC, Description, Knockout)
	SELECT
		MatchID = NEWID()
		, CompetitionID = @CompetitionID
		, MatchDateTime = CAST(Import_Match.MatchDate AS DATETIME) + Import_Match.MatchTime
		, HomeTeamID = HomeTeam.TeamID
		, AwayTeamID = AwayTeam.TeamID
		, MatchPlayed = 0
		, HomeTeamGoals = NULL
		, AwayTeamGoals = NULL
		, NeutralGround = 1
		, HomeTeamTBC = CASE WHEN HomeTeam.TeamID IS NULL THEN Import_Match.HomeTeam ELSE NULL END
		, AwayTeamTBC = CASE WHEN HomeTeam.TeamID IS NULL THEN Import_Match.AwayTeam ELSE NULL END
		, Description = Import_Match.Description
		, Knockout = CASE WHEN NULLIF(Import_Match.HomeTeam, '') IS NULL THEN 1 ELSE 0 END
	FROM
		Import_Match
		LEFT JOIN Team HomeTeam ON Import_Match.HomeTeam = HomeTeam.TeamName
		LEFT JOIN Team AwayTeam ON Import_Match.AwayTeam = AwayTeam.TeamName;

	SELECT
		Home = HomeTeam.ShortName
		, Away = AwayTeam.ShortName
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