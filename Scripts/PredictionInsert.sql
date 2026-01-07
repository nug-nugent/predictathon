USE [predicta_predictathon];

DECLARE @UserID UNIQUEIDENTIFIER = (SELECT UserID FROM [User] WHERE Username = 'mubba liscious');
--DECLARE @UserID UNIQUEIDENTIFIER = (SELECT UserID FROM [User] WHERE EmailAddress = 'dixon_a2@sky.com');

IF @UserID IS NULL
BEGIN
	PRINT 'User not found.';
	RETURN;
END;

SELECT
	HomeTeam = HomeTeam.TeamName
	, AwayTeam = AwayTeam.TeamName
	, *
FROM
	Match
	INNER JOIN Team HomeTeam ON Match.HomeTeamID = HomeTeam.TeamID
	INNER JOIN Team AwayTeam ON Match.AwayTeamID = AwayTeam.TeamID
WHERE 
	--CAST(MatchDateTime AS DATE) = CAST(GETDATE() AS DATE)
	 NOT EXISTS(SELECT 1 FROM Prediction WHERE Prediction.MatchID = Match.MatchID AND Prediction.UserID = @UserID)
	 AND CompetitionID = (SELECT TOP 1 CompetitionID FROM Competition ORDER BY Competition.EndDate DESC)
	 AND Match.MatchDateTime > DATEADD(DAY, -1, GETDATE())
ORDER BY
	Match.MatchDateTime;

RETURN;

--DELETE Prediction WHERE MatchID = '96A24512-298F-4A72-A44E-9FFB8CEC0636' AND UserID = @UserID

INSERT
	Prediction (PredictionID, MatchID, UserID, HomeTeamGoals, AwayTeamGoals, GoalDifference, Score)
SELECT
	NEWID()
	, MatchID = '11366E27-5BA7-4A2A-95E5-BAA5B534AC3C'
	, @UserID
	, HomeTeamGoals = 2
	, AwayTeamGoals = 1
	, GoalDifference = NULL
	, Score = NULL;


--SELECT * FROM Prediction WHERE MatchID = 'A7ECA88B-82C0-44EB-90B7-EB9A3FC367DF' AND UserID = 
--SELECT * FROM [User] WHERE UserID IN (SELECT UserID FROM UserCompetition WHERE CompetitionID = '9EEA2841-CE08-46DE-ADDC-1DBEE9B7DBD2') ORDER BY UserName

--INSERT 
--	Prediction (PredictionID, MatchID, UserID, HomeTeamGoals, AwayTeamGoals, Score, GoalDifference)
--SELECT
--	NEWID()
--	, 'A7ECA88B-82C0-44EB-90B7-EB9A3FC367DF'
--	, 'E2AC9A5C-40E9-4268-96F9-3ACCFDC23367'
--	, 0
--	, 1
--	, 1
--	, -2