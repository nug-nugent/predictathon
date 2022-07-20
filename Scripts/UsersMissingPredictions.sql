USE [predicta_predictathon];

SELECT * FROM Competition WHERE GETDATE() BETWEEN StartDate AND EndDate;
DECLARE @CompetitionID UNIQUEIDENTIFIER = (SELECT TOP 1 CompetitionID FROM Competition WHERE GETDATE() BETWEEN StartDate AND EndDate);
DECLARE @DaysAhead INTEGER = 3;

SELECT 
	[User].Username 
FROM 
	UserCompetition
	INNER JOIN [User] ON UserCompetition.UserID = [User].UserID
WHERE 
	UserCompetition.CompetitionID = @CompetitionID
	--AND x.EmailPredictionReminderDays IS NOT NULL
	AND EXISTS(SELECT 
					1 
				FROM
					Match
					LEFT JOIN (SELECT MatchID FROM Prediction WHERE Prediction.UserID = [User].UserID) UserPrediction ON Match.MatchID = UserPrediction.MatchID
				WHERE
					Match.CompetitionID = @CompetitionID
					AND Match.MatchDateTime BETWEEN GETDATE() AND DATEADD(DAY, @DaysAhead, GETDATE())
					--MatchDateTime <= DATEADD(DAY, x.EmailPredictionReminderDays, MatchDateTime)
					AND UserPrediction.MatchID IS NULL)
ORDER BY 
	LastEmailReminderSent DESC;