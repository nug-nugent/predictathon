USE [predicta_predictathon];

DECLARE @CompetitionName VARCHAR(50) = 'Premier League 2024/25';
DECLARE @CompetitionID UNIQUEIDENTIFIER = (SELECT TOP 1 CompetitionID FROM Competition WHERE CompetitionName = @CompetitionName);
IF @CompetitionID IS NULL
BEGIN
	PRINT '@CompetitionID IS NULL';
	RETURN;
END;

DECLARE @FirstPlaceUserName VARCHAR(50) = 'Serious Bananas';
DECLARE @FirstPlaceUserID UNIQUEIDENTIFIER = (SELECT TOP 1 UserID FROM [User] WHERE UserName = @FirstPlaceUserName);
IF @FirstPlaceUserID IS NULL
BEGIN
	PRINT '@FirstPlaceUserID IS NULL';
	RETURN;
END;

DECLARE @SecondPlaceUserName VARCHAR(50) = 'DarkishJungle';
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
	--ROLLBACK TRANSACTION;
	--COMMIT TRANSACTION;
END;
ELSE
BEGIN
	SELECT 'No action taken - competition already exists in Hall of Fame.';
END;

SELECT Username, EmailAddress, Forenames, Surname FROM [User] u WHERE UserName IN (@FirstPlaceUserName, @SecondPlaceUserName, @ThirdPlaceUserName);

/*
	1st: £125 - Serious Bananas
	2nd: £65  - DarkishJungle
	3rd: £30  - Handsome

Predictathon - prize money
	
Hi,

Well played! I owe you £125: can you please give me a PayPal address I can send it to, or if preferred, your account number and sort code?

All the best,

Nug.

*/

-- SELECT Username, EmailAddress, Forenames, Surname FROM [User] u WHERE UserName IN ('CR Rangers');
