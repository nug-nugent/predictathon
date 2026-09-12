-- =============================================
-- Author:		David Huggett
-- Create date: 19/10/2011
-- Description:	Clears all existing UserCompetitionLeagueHistory entries for the given date and competition, then populates the data,
--				which comprises a snapshot of how the league looked on @Date.
--				None of this is relevant if there were no matches on the given date.
--
--				The snapshot has to be readable next to a live LeagueTableGet row, so it counts the
--				same things that procedure counts: played matches only, and positions ranked across
--				the competition's registrants rather than across every user on the site.
--
-- Update History
--	12/09/2026 - DH - Rank registrants only, count played matches only, and decide whether there's
--					  anything to snapshot without reference to the server clock
-- =============================================
CREATE PROCEDURE [dbo].[UserCompetitionLeagueHistorySet]
	@Date DATE
	, @CompetitionID UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

	--when was the table last updated for this competition?
	DECLARE @MaxLeagueHistoryDate DATE = (SELECT
											MAX([Date])
										FROM
											[dbo].[UserCompetitionLeagueHistory]
											INNER JOIN [dbo].[UserCompetition] ON UserCompetitionLeagueHistory.UserCompetitionID = UserCompetition.UserCompetitionID
										WHERE
											UserCompetition.CompetitionID = @CompetitionID);

	--nothing snapshotted yet, or there've been matches since that date? then get some data in there...
	IF @MaxLeagueHistoryDate IS NULL
		OR EXISTS(SELECT MatchID FROM [dbo].[Match] WHERE Match.CompetitionID = @CompetitionID AND Match.MatchPlayed = 1 AND Match.MatchDateTime >= @MaxLeagueHistoryDate)
	BEGIN
		BEGIN TRY
			BEGIN TRANSACTION;

			DELETE
				[dbo].[UserCompetitionLeagueHistory]
			FROM
				[dbo].[UserCompetitionLeagueHistory]
				INNER JOIN [dbo].[UserCompetition] ON UserCompetitionLeagueHistory.UserCompetitionID = UserCompetition.UserCompetitionID
			WHERE
				[Date] = @Date
				AND UserCompetition.CompetitionID = @CompetitionID;

			INSERT
				UserCompetitionLeagueHistory
				(
					UserCompetitionLeagueHistoryID
					, UserCompetitionID
					, [Date]
					, LeaguePosition
					, Score
					, AverageGoalDifference		
					, TotalGoalDifference		
				)
			SELECT
				NEWID()
				, UserCompetitionID
				, @Date
				, LeaguePosition
				, Score
				, AverageGoalDifference
				, TotalGoalDifference
			FROM
				[dbo].[UserCompetition]
				INNER JOIN (
					SELECT
						[User].Id AS UserID
						, LeaguePosition = ROW_NUMBER() OVER(ORDER BY
															ISNULL(SUM(Prediction.Score), 0) DESC
															, ISNULL(SUM(Prediction.GoalDifference), 0) DESC
															, SUM(CASE WHEN Prediction.Score = 3 THEN 1 ELSE 0 END) DESC
															, SUM(CASE WHEN Prediction.Score = 2 THEN 1 ELSE 0 END) DESC
															, SUM(CASE WHEN Prediction.Score = 1 THEN 1 ELSE 0 END) DESC
															, [User].UserName
														)
						, Score = ISNULL(SUM(Prediction.Score), CAST(0 AS INT))
						, AverageGoalDifference = CAST(ISNULL(AVG(CAST(Prediction.GoalDifference AS DECIMAL(9,2))), CAST(0 AS DECIMAL(9,2))) AS DECIMAL(9,2))
						, TotalGoalDifference = SUM(ISNULL(Prediction.GoalDifference, 0))
					FROM
						[Identity].[Users] AS [User]
						--ranked across this competition's registrants only: an outsider taking up a
						--rank here would put every snapshot position out of step with LeagueTableGet's
						INNER JOIN [dbo].[UserCompetition] AS Registration ON [User].Id = Registration.UserID AND Registration.CompetitionID = @CompetitionID
						CROSS JOIN [dbo].[Match]
						LEFT JOIN [dbo].[Prediction] ON Match.MatchID = Prediction.MatchID AND [User].Id = Prediction.UserID
					WHERE
						CAST(Match.MatchDateTime AS DATE) <= @Date
						AND Match.CompetitionID = @CompetitionID
						AND Match.MatchPlayed = 1
					GROUP BY
						[User].UserName
						, [User].Id
				) LeagueTable ON UserCompetition.UserID = LeagueTable.UserID
			WHERE
				UserCompetition.CompetitionID = @CompetitionID
			ORDER BY
				LeaguePosition;

			COMMIT TRANSACTION;
		END TRY
		BEGIN CATCH
			IF @@TRANCOUNT > 0
			BEGIN
				ROLLBACK TRANSACTION;
			END;

			THROW;
		END CATCH;
	END;
END;
