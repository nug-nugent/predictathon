USE [predicta_predictathon];

DECLARE @CompetitionName VARCHAR(50) = 'Premier League 2021/22';
DECLARE @CompetitionID UNIQUEIDENTIFIER = (SELECT TOP 1 CompetitionID FROM Competition WHERE CompetitionName = @CompetitionName);
IF @CompetitionID IS NULL
BEGIN
	PRINT '@CompetitionID IS NULL';
	RETURN;
END;

DECLARE @FirstPlaceUserName VARCHAR(50) = 'DarkishJungle';
DECLARE @FirstPlaceUserID UNIQUEIDENTIFIER = (SELECT TOP 1 UserID FROM [User] WHERE UserName = @FirstPlaceUserName);
IF @FirstPlaceUserID IS NULL
BEGIN
	PRINT '@FirstPlaceUserID IS NULL';
	RETURN;
END;

DECLARE @SecondPlaceUserName VARCHAR(50) = 'CR Rangers';
DECLARE @SecondPlaceUserID UNIQUEIDENTIFIER = (SELECT TOP 1 UserID FROM [User] WHERE UserName = @SecondPlaceUserName);
IF @SecondPlaceUserID IS NULL
BEGIN
	PRINT '@SecondPlaceUserID IS NULL';
	RETURN;
END;

DECLARE @ThirdPlaceUserName VARCHAR(50) = 'Handsome';
DECLARE @ThirdPlaceUserID UNIQUEIDENTIFIER = (SELECT TOP 1 UserID FROM [User] WHERE UserName = @ThirdPlaceUserName);
IF @ThirdPlaceUserID IS NULL
BEGIN
	PRINT '@ThirdPlaceUserID IS NULL';
	RETURN;
END;

IF NOT EXISTS(SELECT 1 FROM HallOfFame WHERE CompetitionID = @CompetitionID)
BEGIN
	--BEGIN TRANSACTION
		INSERT
			HallOfFame (HallOfFameID, CompetitionID, CompetitionName, Winner, WinnerUserID, SecondPlace, SecondPlaceUserID, ThirdPlace, ThirdPlaceUserID, EndDate, ImageFilename)
		SELECT
			NEWID()
			, @CompetitionID
			, @CompetitionName
			, @FirstPlaceUserName
			, @FirstPlaceUserID
			, @SecondPlaceUserName
			, @SecondPlaceUserID
			, @ThirdPlaceUserName
			, @ThirdPlaceUserID
			, Competition.EndDate
			, Competition.ImageFilename
		FROM
			Competition
		WHERE
			CompetitionID = @CompetitionID;
	--COMMIT TRANSACTION;
END;

SELECT Username, EmailAddress, Forenames, Surname FROM [User] u WHERE UserName IN (@FirstPlaceUserName, @SecondPlaceUserName, @ThirdPlaceUserName);

/*
	1st:  £125 - DarkishJungle - £115 sent, I owe a Premier League code
	2nd: £65 - CR Rangers - emailed
	3rd: £30 - Handsome - emailed

	Predictathon - prize money
	
	Hi,
	
	Well played! I owe you £25: can you please send me your account number and sort code?
	If you're keen to play the upcoming Euros and you're happy for me to hold onto £10 for that, please let me know.
	
	All the best,

	Nug.
*/