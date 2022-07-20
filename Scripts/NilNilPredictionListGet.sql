SELECT 
	[User].Username
	, Match.MatchDateTime
	, HomeTeam = HomeTeam.TeamName
	, Match.HomeTeamGoals
	, AwayTeam = AwayTeam.TeamName
	, Match.AwayTeamGoals
	, PredictionScore = Prediction.Score
	, Prediction.GoalDifference 
FROM 
	Prediction
	INNER JOIN [User] ON Prediction.UserID = [User].UserID
	INNER JOIN Match ON Prediction.MatchID = Match.MatchID
	INNER JOIN Team HomeTeam ON Match.HomeTeamID = HomeTeam.TeamID
	INNER JOIN Team AwayTeam ON Match.AwayTeamID = AwayTeam.TeamID
 WHERE 
	Prediction.HomeTeamGoals = 0 AND Prediction.AwayTeamGoals = 0
ORDER BY
	Prediction.Score DESC
	, Prediction.GoalDifference DESC;