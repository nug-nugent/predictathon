USE [predicta_predictathon];

BEGIN TRAN
	DECLARE @CompetitionName VARCHAR(50) = 'Premier League 2024/25';
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
			, StartDate = (SELECT MIN(MatchDate) FROM Import_Match)
			, EndDate = (SELECT MAX(MatchDate) FROM Import_Match)
		FROM
			Competition
		WHERE
			CompetitionName = 'Premier League 2023/24'

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
	INSERT @tblTeam SELECT DISTINCT TeamID FROM Team INNER JOIN Import_Match ON Import_Match.HomeTeam = Team.TeamName;

	SELECT DISTINCT HomeTeam FROM Import_Match WHERE HomeTeam NOT IN (SELECT TeamName FROM Team);
	
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
		INNER JOIN Team AwayTeam ON Import_Match.AwayTeam = AwayTeam.TeamName;

	SELECT * FROM Match WHERE CompetitionID = @CompetitionID ORDER BY MatchDateTime;
ROLLBACK;