-- Hall of Fame queries:

--	Highest percentage of 'correct' predictions (win/lose/draw only)
 SELECT TOP 10
 	Username
	, UserID
	, (CAST(CorrectPredictions AS DECIMAL(9, 2)) / CAST(TotalPredictions AS DECIMAL(9, 2))) * 100
FROM
	(
	SELECT
		[User].Username
		, [User].UserID
		, TotalPredictions = COUNT(1)
		, CorrectPredictions = SUM(CASE WHEN Prediction.Score > 0 THEN 1 ELSE 0 END)
	FROM
		[User]
		INNER JOIN Prediction ON [User].UserID = Prediction.UserID
	WHERE
		Prediction.Score IS NOT NULL
	GROUP BY
		[User].Username
		, [User].UserID
	) PredictionCount
WHERE
	PredictionCount.CorrectPredictions > 0
ORDER BY
	CAST(CorrectPredictions AS DECIMAL(9, 2)) / CAST(TotalPredictions AS DECIMAL(9, 2)) DESC;

--	Highest average score per prediction
 SELECT TOP 10
	[User].Username
	, [User].UserID
	, AverageScore = CAST(SUM(Prediction.Score) AS DECIMAL(9, 2)) / COUNT(1.00)
FROM
	[User]
	INNER JOIN Prediction ON [User].UserID = Prediction.UserID
GROUP BY
	[User].Username
	, [User].UserID
ORDER BY
	SUM(Prediction.Score) DESC;

-- Total all-time points
SELECT TOP 10
	[User].Username
	, [User].UserID
	, TotalScore = SUM(Prediction.Score)
FROM
	[User]
	INNER JOIN Prediction ON [User].UserID = Prediction.UserID
GROUP BY
	[User].Username
	, [User].UserID
ORDER BY
	SUM(Prediction.Score) DESC;

-- Users to have predicted the most matches altogether
SELECT TOP 10
	[User].Username
	, [User].UserID
	, TotalPredictions = COUNT(1)
FROM
	[User]
	INNER JOIN Prediction ON [User].UserID = Prediction.UserID
GROUP BY
	[User].Username
	, [User].UserID
ORDER BY
	COUNT(1) DESC;

-- Most overall wins/2nds/3rds
SELECT
	[User].Username
	, [User].UserID
	, Wins = (SELECT COUNT(1) FROM HallOfFame WHERE WinnerUserID = [User].UserID)
	, SecondPlaces = (SELECT COUNT(1) FROM HallOfFame WHERE SecondPlaceUserID = [User].UserID)
	, ThirdPlaces = (SELECT COUNT(1) FROM HallOfFame WHERE ThirdPlaceUserID = [User].UserID)
FROM
	[User]
GROUP BY
	[User].Username
	, [User].UserID
ORDER BY
	Wins DESC
	, SecondPlaces DESC
	, ThirdPlaces DESC;
