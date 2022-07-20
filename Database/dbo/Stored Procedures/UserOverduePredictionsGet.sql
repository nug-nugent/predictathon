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
		[User].Username
		, [User].UserID
		, PredictionsDue.UserCompetitionID
		, [User].EmailAddress
		, PredictionsDue.CompetitionName
		, PredictionsDue.NextPredictionDue
	FROM
		(
		SELECT
			UserCompetition.UserID
			, UserCompetition.UserCompetitionID
			, UserCompetition.LastEmailReminderSent
			, Competition.CompetitionName
			, NextPredictionDue = MIN(FutureMatch.MatchDateTime)
		FROM
			UserCompetition
			INNER JOIN Competition ON UserCompetition.CompetitionID = Competition.CompetitionID
			INNER JOIN (SELECT MatchID, CompetitionID, MatchDateTime FROM Match WHERE Match.MatchDateTime > GETDATE()) FutureMatch ON Competition.CompetitionID = FutureMatch.CompetitionID
			LEFT JOIN Prediction ON FutureMatch.MatchID = Prediction.MatchID AND UserCompetition.UserID = Prediction.UserID
		WHERE
			Prediction.PredictionID IS NULL
		GROUP BY
			UserCompetition.UserID
			, UserCompetition.UserCompetitionID
			, UserCompetition.LastEmailReminderSent
			, Competition.CompetitionName) PredictionsDue
		INNER JOIN [User] ON PredictionsDue.UserID = [User].UserID
	WHERE
		[User].EmailPredictionReminderDays IS NOT NULL
		AND CAST(NextPredictionDue AS DATE) <= CAST(DATEADD(DAY, [User].EmailPredictionReminderDays, GETDATE()) AS DATE)
		AND (LastEmailReminderSent IS NULL OR CAST(LastEmailReminderSent AS DATE) < CAST(DATEADD(DAY, -[User].EmailPredictionReminderDays, NextPredictionDue) AS DATE))
	ORDER BY
		[User].UserID
		, PredictionsDue.UserCompetitionID
END
