SELECT TOP 10
	Match.MatchID
	, HomeTeam.ShortName + ' vs ' + AWayTeam.ShortName AS [Match]
	, CAST(Match.MatchDateTime AS DATE) AS MatchDate
	, P.Prediction
	, ActualResult = ISNULL(CAST(HomeTeamGoals AS VARCHAR(10)) + '-' + CAST(AWayTeamGoals AS VARCHAR(10)), '?')
	, P.NumberOfPredictions
FROM
	Match
	INNER JOIN (
		SELECT
			Prediction = CAST(HomeTeamGoals AS VARCHAR(10)) + '-' + CAST(AWayTeamGoals AS VARCHAR(10))
			, MatchID
			, NumberOfPredictions = COUNT(1)
		FROM
			Prediction
		GROUP BY
			MatchID
			, CAST(HomeTeamGoals AS VARCHAR(10)) + '-' + CAST(AWayTeamGoals AS VARCHAR(10))
	) P ON MAtch.MatchID = P.MatchID
	LEFT JOIN TEam HomeTeam ON MAtch.HomeTeamID = HomeTeam.TeamID
	LEFT JOIN TEam AwayTeam ON MAtch.AWayTeamID = AwayTeam.TeamID
ORDER BY
	P.NumberOfPredictions DESC
	;