USE [predicta_predictathon];

DECLARE @CompetitionName VARCHAR(50) = 'Premier League 2025/26';
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

DECLARE @SecondPlaceUserName VARCHAR(50) = 'No Drawers';
DECLARE @SecondPlaceUserID UNIQUEIDENTIFIER = (SELECT TOP 1 UserID FROM [User] WHERE UserName = @SecondPlaceUserName);
IF @SecondPlaceUserID IS NULL
BEGIN
	PRINT '@SecondPlaceUserID IS NULL';
	RETURN;
END;

DECLARE @ThirdPlaceUserName VARCHAR(50) = 'Miramar Majestics';
DECLARE @ThirdPlaceUserID UNIQUEIDENTIFIER = (SELECT TOP 1 UserID FROM [User] WHERE UserName = @ThirdPlaceUserName);
IF @ThirdPlaceUserID IS NULL
BEGIN
	PRINT '@ThirdPlaceUserID IS NULL';
	RETURN;
END;

IF NOT EXISTS(SELECT 1 FROM HallOfFame WHERE CompetitionID = @CompetitionID)
BEGIN
	BEGIN TRANSACTION
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
	ROLLBACK TRANSACTION;
	--COMMIT TRANSACTION;
END;
ELSE
BEGIN
	SELECT 'No action taken - competition already exists in Hall of Fame.';
END;

SELECT Username, EmailAddress, Forenames, Surname FROM [User] u WHERE UserName IN (@FirstPlaceUserName, @SecondPlaceUserName, @ThirdPlaceUserName);

/*
	1st: £155 - DarkishJungle
	2nd: £75  - No Drawers
	3rd: £40  - Miramar Majestics

Predictathon - prize money
	
Hi,

Well played! I owe you £155: can you please give me a PayPal address I can send it to, or if preferred, your account number and sort code?

All the best,

Nug.

*/

-- SELECT Username, EmailAddress, Forenames, Surname FROM [User] u WHERE UserName IN ('CR Rangers');
