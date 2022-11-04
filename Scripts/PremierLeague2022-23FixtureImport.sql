USE [predicta_predictathon];

BEGIN TRAN
	DECLARE @CompetitionName VARCHAR(50) = 'Premier League 2022/23';
	DECLARE @CompetitionID UNIQUEIDENTIFIER = (SELECT CompetitionID FROM Competition WHERE CompetitionName = @CompetitionName);

	IF @CompetitionID IS NULL
	BEGIN
		SET @CompetitionID = NEWID();

		INSERT
			Competition (
				CompetitionID, CompetitionName, AllowTwoPointers, DefaultToNeutralGround
				, DuplicateFixturesAllowed, EntranceFee, ImageFilename, Information, OpenForRegistration, PayPalPaymentAvailable
				, PrependNameWithThe, RegistrationAvailableOnLoginPage, ShowInHallOfFame, StartDate, EndDate)
		SELECT
			@CompetitionID
			, @CompetitionName
			, AllowTwoPointers
			, DefaultToNeutralGround
			, DuplicateFixturesAllowed
			, EntranceFee
			, ImageFilename
			, Information
			, 1
			, PayPalPaymentAvailable
			, PrependNameWithThe
			, 1
			, ShowInHallOfFame
			, StartDate = (SELECT MIN(Date) FROM Import_Match)
			, EndDate = (SELECT MAX(Date) FROM Import_Match)
		FROM
			Competition
		WHERE
			CompetitionName = 'Premier League 2021/22'

		IF @@ROWCOUNT = 0 
		BEGIN
			PRINT '@CompetitionID IS NULL';
			ROLLBACK TRANSACTION;
			RETURN;
		END;
	END;

	DELETE Match WHERE CompetitionID = @CompetitionID;
	DELETE TeamCompetition WHERE CompetitionID = @CompetitionID;
	
	SET DATEFIRST 1;

	DECLARE @tblTeam TABLE (TeamID UNIQUEIDENTIFIER);
	INSERT @tblTeam SELECT DISTINCT TeamID FROM Team INNER JOIN Import_Match ON Import_Match.Home = Team.TeamName;

	SELECT DISTINCT Home FROM Import_Match WHERE Home NOT IN (SELECT TeamName FROM Team);
	
	IF @@ROWCOUNT > 0 
	BEGIN
		PRINT 'Some teams don''t exist - script aborted.';
		ROLLBACK TRANSACTION;
		RETURN;
	END;
	
	INSERT TeamCompetition (TeamCompetitionID, CompetitionID, TeamID) SELECT NEWID(), @CompetitionID, TeamID FROM @tblTeam

	INSERT
		Match (MatchID, CompetitionID, MatchDateTime, HomeTeamID, AwayTeamID, MatchPlayed, HomeTeamGoals, AwayTeamGoals, NeutralGround, HomeTeamTBC, AwayTeamTBC, Description, Knockout)
	SELECT
		MatchID = NEWID()
		, CompetitionID = @CompetitionID
		, CAST(Import_Match.Date AS DATETIME) + Import_Match.Time
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
		INNER JOIN Team HomeTeam ON Import_Match.Home = HomeTeam.TeamName
		INNER JOIN Team AwayTeam ON Import_Match.Away = AwayTeam.TeamName;

	--UPDATE Match SET MatchDateTime = DATEADD(HOUR, 15, MatchDateTime) WHERE Match.CompetitionID = @CompetitionID AND DATEPART(WEEKDAY, MatchDateTime) = 6
	--UPDATE Match SET MatchDateTime = DATEADD(HOUR, 16, MatchDateTime) WHERE Match.CompetitionID = @CompetitionID AND DATEPART(WEEKDAY, MatchDateTime) = 7
	--UPDATE Match SET MatchDateTime = DATEADD(HOUR, 20, MatchDateTime) WHERE Match.CompetitionID = @CompetitionID AND DATEPART(WEEKDAY, MatchDateTime) <= 5

	SELECT * FROM Match WHERE CompetitionID = @CompetitionID ORDER BY MatchDateTime;
ROLLBACK;