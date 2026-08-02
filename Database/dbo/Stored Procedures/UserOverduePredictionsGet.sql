-- =============================================
-- Author:		David Huggett
-- Create date: 14/01/2012
-- Description:	Returns a list of users and their competitions, where the user has a prediction due within [User.EmailPredictionReminderDays] days of the current date.
-- =============================================
CREATE PROCEDURE [dbo].[UserOverduePredictionsGet]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT
		[User].UserName AS Username
		, [User].Id AS UserID
		, PredictionsDue.UserCompetitionID
		, [User].Email AS EmailAddress
		, PredictionsDue.CompetitionName
		, PredictionsDue.NextPredictionDue
	FROM
		(
		SELECT
			uc.UserID
			, uc.UserCompetitionID
			, uc.LastEmailReminderSent
			, c.CompetitionName
			, NextPredictionDue = MIN(FutureMatch.MatchDateTime)
		FROM
			[dbo].[UserCompetition] AS uc
			INNER JOIN [dbo].[Competition] AS c ON uc.CompetitionID = c.CompetitionID
			INNER JOIN (SELECT m.MatchID, m.CompetitionID, m.MatchDateTime FROM [dbo].[Match] AS m WHERE m.MatchDateTime > GETDATE()) FutureMatch ON c.CompetitionID = FutureMatch.CompetitionID
			LEFT JOIN [dbo].[Prediction] AS p ON FutureMatch.MatchID = p.MatchID AND uc.UserID = p.UserID
		WHERE
			p.PredictionID IS NULL
		GROUP BY
			uc.UserID
			, uc.UserCompetitionID
			, uc.LastEmailReminderSent
			, c.CompetitionName) PredictionsDue
		INNER JOIN [Identity].[Users] AS [User] ON PredictionsDue.UserID = [User].Id
	WHERE
		[User].EmailPredictionReminderDays IS NOT NULL
		AND CAST(PredictionsDue.NextPredictionDue AS DATE) <= CAST(DATEADD(DAY, [User].EmailPredictionReminderDays, GETDATE()) AS DATE)
		AND (PredictionsDue.LastEmailReminderSent IS NULL OR CAST(PredictionsDue.LastEmailReminderSent AS DATE) < CAST(DATEADD(DAY, -[User].EmailPredictionReminderDays, PredictionsDue.NextPredictionDue) AS DATE))
	ORDER BY
		[User].Id
		, PredictionsDue.UserCompetitionID
END
