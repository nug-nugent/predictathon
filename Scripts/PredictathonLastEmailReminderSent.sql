SELECT LastEmailReminderSent, Username, * FROM UserCompetition INNER JOIN [User] ON UserCompetition.UserID = [User].UserID ORDER BY UserCompetition.LastEmailReminderSent DESC

SELECT LastLoginDateTime, Username FROM [User] ORDER BY [User].LastLoginDateTime DESC

EXEC UserOverduePredictionsGet
--overdue predictions - 1/11/12:
--BubbaGump!
--Suzan Ham - NO....
--Nugsson!
--heva!
--astronautis!
--swingitinthemixer!

SELECT Username FROM [User] WHERE UserID NOT IN (SELECT UserID FROM Prediction WHERE Prediction.MatchID = '03CD6492-CE6A-44DF-8035-BD7F283B84C0')

SELECT
	MatchDateTime
	, HomeTeam.ShortName AS HomeTeam
	, AwayTeam.ShortName AS AwayTeam
	, Match.MatchID
FROM 
	Match 
	LEFT JOIN Team HomeTeam ON Match.HomeTeamID = HomeTeam.TeamID
	LEFT JOIN Team AwayTeam ON Match.AwayTeamID = AwayTeam.TeamID
WHERE 
	MatchPlayed = 0 
ORDER BY 
	Match.MatchDateTime ASC