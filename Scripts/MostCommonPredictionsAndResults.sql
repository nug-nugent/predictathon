DECLARE @CompetitionID UNIQUEIDENTIFIER = '010446B5-E013-4F2F-8DF4-4AC2D9E63C58' --SELECT * FROM Competition ORDER BY StartDate
SELECT
	Prediction = CAST(Prediction.HomeTeamGoals AS VARCHAR(2)) + '-' + CAST(Prediction.AwayTeamGoals AS VARCHAR(2))
	, NumberOfPredictions = COUNT(1)
	, AverageScore = AVG(CAST(Prediction.Score AS DECIMAL(9, 2)))
	, [User].Username
FROM
	Prediction
	INNER JOIN Match ON Prediction.MatchID = Match.MatchID
	INNER JOIN [User] ON Prediction.UserID = [User].UserID
WHERE
	1 = 1
	AND Prediction.Score IS NOT NULL
	AND [User].Username IN ('Nugsson', 'WiztipskiMor', 'Hamster64')
	AND (@CompetitionID IS NULL OR Match.CompetitionID = @CompetitionID)
GROUP BY
	CAST(Prediction.HomeTeamGoals AS VARCHAR(2)) + '-' + CAST(Prediction.AwayTeamGoals AS VARCHAR(2))
	, [User].Username
ORDER BY
	Prediction
	, Username
	, AverageScore DESC


-- Most common scorelines
SELECT
	Result = CAST(HomeTeamGoals AS VARCHAR(2)) + '-' + CAST(AwayTeamGoals AS VARCHAR(2))
	, NumberOfMatches = COUNT(1)
FROM
	Match
WHERE
	MatchPlayed = 1
	AND (@CompetitionID IS NULL OR Match.CompetitionID = @CompetitionID)
GROUP BY
	CAST(HomeTeamGoals AS VARCHAR(2)) + '-' + CAST(AwayTeamGoals AS VARCHAR(2))
ORDER BY
	NumberOfMatches DESC
